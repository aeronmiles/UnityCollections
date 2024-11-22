import AVFoundation
import UIKit

@objc
class CameraCapture: NSObject, AVCapturePhotoCaptureDelegate,
  AVCaptureVideoDataOutputSampleBufferDelegate
{
  // MARK: - Properties

  private var captureSession: AVCaptureSession?
  private var photoOutput: AVCapturePhotoOutput?
  private var videoDataOutput: AVCaptureVideoDataOutput?
  private var previewLayer: AVCaptureVideoPreviewLayer?
  private var currentCameraPosition: AVCaptureDevice.Position = .back
  private var lastFrameTime: TimeInterval = 0
  private var gameObjectName: String?
  private var currentVideoOrientation: AVCaptureVideoOrientation = .portrait
  private var isVideoMirrored: Bool = false
  private var isPaused: Bool = false

  // Thread safety
  private let sessionQueue = DispatchQueue(label: "com.camera.sessionQueue")
  private let captureQueue = DispatchQueue(label: "com.camera.captureQueue")
  private let videoProcessingQueue = DispatchQueue(label: "com.camera.videoProcessingQueue")
  private var isCapturing = false

  // Camera settings
  private var clampedTemperature: Float = 2500
  private var whiteBalanceMode: AVCaptureDevice.WhiteBalanceMode = .continuousAutoWhiteBalance

  // Resource tracking
  private var currentCameraInput: AVCaptureDeviceInput?
  private var isConfiguring = false

  // Double buffering for preview frames
  private var previewBuffers: [UnsafeMutablePointer<UInt8>?] = [nil, nil]
  private var previewBufferSizes: [Int] = [0, 0]
  private var currentPreviewBufferIndex = 0
  private var previewBufferReady: [Bool] = [false, false]

  // Double buffering for photo captures
  private var photoBuffers: [UnsafeMutablePointer<UInt8>?] = [nil, nil]
  private var photoBufferSizes: [Int] = [0, 0]
  private var currentPhotoBufferIndex = 0
  private var photoBufferReady: [Bool] = [false, false]
  private let bufferResizeLock = DispatchQueue(label: "com.camera.bufferResize")
  private let minBufferSize = 1024 * 1024  // 1MB minimum
  private let maxBufferSize = 100 * 1024 * 1024  // 100MB maximum

  static let shared = CameraCapture()

  private override init() {
    super.init()

    // Setup notifications for app lifecycle
    NotificationCenter.default.addObserver(
      self,
      selector: #selector(handleAppWillResignActive),
      name: UIApplication.willResignActiveNotification,
      object: nil
    )

    NotificationCenter.default.addObserver(
      self,
      selector: #selector(handleAppDidBecomeActive),
      name: UIApplication.didBecomeActiveNotification,
      object: nil
    )

    NotificationCenter.default.addObserver(
      self,
      selector: #selector(handleMemoryWarning),
      name: UIApplication.didReceiveMemoryWarningNotification,
      object: nil
    )
  }

  deinit {
    NotificationCenter.default.removeObserver(self)
    cleanup()
  }

  // MARK: - Public Interface

  @objc func initializeCamera(gameObjectName: String) {
    self.gameObjectName = gameObjectName
    sessionQueue.async { [weak self] in
      self?.setupCaptureSession()
    }
  }

  @objc func startPreview() {
    sessionQueue.async { [weak self] in
      guard let self = self else { return }
      print("CameraCapture.swift :: Starting preview")
      self.captureSession?.startRunning()
    }
  }

  @objc func pausePreview() {
    sessionQueue.async { [weak self] in
      guard let self = self else { return }
      print("CameraCapture.swift :: Pausing preview")
      self.isPaused = true
      self.captureSession?.stopRunning()

      DispatchQueue.main.async {
        UnityBridge.sendMessage(
          toGameObject: self.gameObjectName,
          methodName: "OnPreviewPaused",
          message: ""
        )
      }
    }
  }

  @objc func resumePreview() {
    sessionQueue.async { [weak self] in
      guard let self = self else { return }
      print("CameraCapture.swift :: Resuming preview")
      self.isPaused = false
      self.captureSession?.startRunning()

      DispatchQueue.main.async {
        UnityBridge.sendMessage(
          toGameObject: self.gameObjectName,
          methodName: "OnPreviewResumed",
          message: ""
        )
      }
    }
  }

  @objc func takePhoto() {
    videoProcessingQueue.async { [weak self] in
      guard let self = self else { return }

      // Check if we're already capturing
      guard !self.isCapturing else {
        print("CameraCapture.swift :: Photo capture already in progress")
        self.photoOutputError(errorMsg: "Photo capture already in progress")
        return
      }

      // Set capturing state
      self.isCapturing = true

      print("CameraCapture.swift :: Taking photo")
      guard let photoOutput = self.photoOutput else {
        print("CameraCapture.swift :: Photo output not available")
        self.isCapturing = false
        self.photoOutputError(errorMsg: "Photo output not available")
        return
      }

      self.updateVideoOrientation()

      let settings = AVCapturePhotoSettings()
      settings.isAutoStillImageStabilizationEnabled = true
      settings.flashMode = .off  // Set as needed

      self.photoOutput?.capturePhoto(with: settings, delegate: self)
    }
  }

  @objc func switchCamera() {
    sessionQueue.async { [weak self] in
      guard let self = self else { return }
      print("CameraCapture.swift :: Switching camera")

      guard !self.isConfiguring else {
        print("CameraCapture.swift :: Camera switch in progress")
        return
      }

      self.isConfiguring = true

      do {
        try self.performCameraSwitch()
      } catch {
        print("CameraCapture.swift :: Error switching camera: \(error.localizedDescription)")
      }

      self.isConfiguring = false
    }
  }

  @objc func stopCamera() {
    sessionQueue.async { [weak self] in
      guard let self = self else { return }
      print("CameraCapture.swift :: Stopping camera")
      self.cleanup()

      DispatchQueue.main.async {
        UnityBridge.sendMessage(
          toGameObject: self.gameObjectName,
          methodName: "OnCameraStopped",
          message: ""
        )
      }
    }
  }

  @objc func setWhiteBalanceMode(_ mode: Int) {
    guard
      let device = AVCaptureDevice.default(
        .builtInWideAngleCamera,
        for: .video,
        position: currentCameraPosition
      ),
      let whiteBalanceMode = AVCaptureDevice.WhiteBalanceMode(rawValue: mode),
      device.isWhiteBalanceModeSupported(whiteBalanceMode)
    else {
      print("CameraCapture.swift :: Selected mode is not supported.")
      return
    }

    do {
      try device.lockForConfiguration()
      device.whiteBalanceMode = whiteBalanceMode

      if whiteBalanceMode == .locked {
        // Optionally, set a specific temperature when locking
        let temperatureAndTint = AVCaptureDevice.WhiteBalanceTemperatureAndTintValues(
          temperature: self.clampedTemperature, tint: 0)
        let gains = device.deviceWhiteBalanceGains(for: temperatureAndTint)
        device.setWhiteBalanceModeLocked(with: gains, completionHandler: nil)
      }

      device.unlockForConfiguration()
      print("CameraCapture.swift :: Color temperature mode set to \(whiteBalanceMode.rawValue).")

      // Update the instance property
      self.whiteBalanceMode = whiteBalanceMode
    } catch {
      print(
        "CameraCapture.swift :: Failed to set color temperature mode: \(error.localizedDescription)"
      )
    }
  }

  @objc func setColorTemperature(_ temperature: Float) {
    // Clamp temperature to supported range (2500 K to 7500 K for example)
    clampedTemperature = max(2500, min(temperature, 7500))

    sessionQueue.async { [weak self] in
      guard let self = self else { return }

      guard
        let device = AVCaptureDevice.default(
          .builtInWideAngleCamera,
          for: .video,
          position: self.currentCameraPosition
        )
      else { return }

      do {
        try self.configureWhiteBalance(device: device, temperature: self.clampedTemperature)
        print(
          "CameraCapture.swift :: Set color temperature to \(self.clampedTemperature)K for photo and video capture."
        )
      } catch {
        print(
          "CameraCapture.swift :: Error setting color temperature: \(error.localizedDescription)")
      }
    }
  }

  // MARK: - Memory Management
  // Update cleanup to use safe deallocation
  private func cleanup() {
    bufferResizeLock.sync {
      captureSession?.stopRunning()

      // Clean up capture session
      if let inputs = captureSession?.inputs {
        for input in inputs {
          captureSession?.removeInput(input)
        }
      }

      if let outputs = captureSession?.outputs {
        for output in outputs {
          captureSession?.removeOutput(output)
        }
      }

      // Clean up preview layer
      previewLayer?.removeFromSuperlayer()
      previewLayer = nil

      // Safely deallocate preview buffers
      for i in 0..<previewBuffers.count {
        safelyDeallocateBuffer(&previewBuffers[i])
        previewBufferSizes[i] = 0
        previewBufferReady[i] = false
      }

      // Safely deallocate photo buffers
      for i in 0..<photoBuffers.count {
        safelyDeallocateBuffer(&photoBuffers[i])
        photoBufferSizes[i] = 0
        photoBufferReady[i] = false
      }

      // Clear references
      captureSession = nil
      photoOutput = nil
      videoDataOutput = nil
      currentCameraInput = nil
    }
  }

  // MARK: - Private Setup Methods

  private func setupCaptureSession() {
    print("CameraCapture.swift :: Setting up capture session")
    cleanup()  // Clean up any existing session

    captureSession = AVCaptureSession()
    guard let captureSession = captureSession else { return }

    captureSession.beginConfiguration()
    defer {
      captureSession.commitConfiguration()

      DispatchQueue.main.async { [weak self] in
        guard let self = self else { return }
        UnityBridge.sendMessage(
          toGameObject: self.gameObjectName,
          methodName: "OnCameraInitialized",
          message: ""
        )
      }
    }

    // Configure session preset
    if captureSession.canSetSessionPreset(.photo) {
      captureSession.sessionPreset = .photo
    }

    // Setup camera input
    guard
      let camera = AVCaptureDevice.default(
        .builtInWideAngleCamera,
        for: .video,
        position: currentCameraPosition)
    else {
      print("CameraCapture.swift :: Failed to get camera device")
      return
    }

    do {
      let input = try AVCaptureDeviceInput(device: camera)
      guard captureSession.canAddInput(input) else {
        print("CameraCapture.swift :: Cannot add camera input")
        return
      }
      currentCameraInput = input
      captureSession.addInput(input)
    } catch {
      print("CameraCapture.swift :: Error setting up camera input: \(error.localizedDescription)")
      return
    }

    // Setup photo output
    photoOutput = AVCapturePhotoOutput()
    if let photoOutput = photoOutput,
      captureSession.canAddOutput(photoOutput)
    {
      photoOutput.isHighResolutionCaptureEnabled = true
      captureSession.addOutput(photoOutput)
    }

    // Setup video output
    videoDataOutput = AVCaptureVideoDataOutput()
    videoDataOutput?.setSampleBufferDelegate(self, queue: videoProcessingQueue)

    if let videoDataOutput = videoDataOutput {
      videoDataOutput.videoSettings = [
        kCVPixelBufferPixelFormatTypeKey as String: kCVPixelFormatType_32BGRA
      ]
      videoDataOutput.alwaysDiscardsLateVideoFrames = true

      if captureSession.canAddOutput(videoDataOutput) {
        captureSession.addOutput(videoDataOutput)
      }
    }

    // Allocate double buffers for preview frames
    let maxPreviewBufferSize = calculateMaxPreviewBufferSize()
    for i in 0..<2 {
      previewBuffers[i] = UnsafeMutablePointer<UInt8>.allocate(capacity: maxPreviewBufferSize)
      previewBufferSizes[i] = maxPreviewBufferSize
      previewBufferReady[i] = false
    }

    // Allocate double buffers for photo captures
    let maxPhotoBufferSize = calculateMaxPhotoBufferSize()
    for i in 0..<2 {
      photoBuffers[i] = UnsafeMutablePointer<UInt8>.allocate(capacity: maxPhotoBufferSize)
      photoBufferSizes[i] = maxPhotoBufferSize
      photoBufferReady[i] = false
    }

    previewLayer = AVCaptureVideoPreviewLayer(session: captureSession)
  }

  private func performCameraSwitch() throws {
    guard let captureSession = captureSession else { return }

    captureSession.beginConfiguration()
    defer { captureSession.commitConfiguration() }

    // Remove current input
    if let currentInput = currentCameraInput {
      captureSession.removeInput(currentInput)
    }

    // Switch position
    currentCameraPosition = currentCameraPosition == .back ? .front : .back

    // Add new input
    guard
      let newCamera = AVCaptureDevice.default(
        .builtInWideAngleCamera,
        for: .video,
        position: currentCameraPosition),
      let newInput = try? AVCaptureDeviceInput(device: newCamera),
      captureSession.canAddInput(newInput)
    else {
      throw NSError(
        domain: "CameraCapture",
        code: -1,
        userInfo: [NSLocalizedDescriptionKey: "Failed to switch camera"])
    }

    currentCameraInput = newInput
    captureSession.addInput(newInput)
  }

  private func configureWhiteBalance(device: AVCaptureDevice, temperature: Float) throws {
    try device.lockForConfiguration()
    defer { device.unlockForConfiguration() }

    if device.isWhiteBalanceModeSupported(.locked) {
      let temperatureAndTint = AVCaptureDevice.WhiteBalanceTemperatureAndTintValues(
        temperature: temperature, tint: 0)
      let gains = device.deviceWhiteBalanceGains(for: temperatureAndTint)
      let normalizedGains = normalizeGains(gains, for: device)
      device.setWhiteBalanceModeLocked(with: normalizedGains, completionHandler: nil)
    }
  }

  // MARK: - Orientation Management

  private func updateVideoOrientation() {
    let deviceOrientation = UIDevice.current.orientation
    currentVideoOrientation = AVCaptureVideoOrientation(deviceOrientation: deviceOrientation)

    if let connection = photoOutput?.connection(with: .video) {
      if connection.isVideoOrientationSupported {
        connection.videoOrientation = currentVideoOrientation
      }
    }
  }

  // MARK: - App Lifecycle Handlers

  @objc private func handleAppWillResignActive() {
    sessionQueue.async { [weak self] in
      self?.pausePreview()
    }
  }

  @objc private func handleAppDidBecomeActive() {
    sessionQueue.async { [weak self] in
      self?.resumePreview()
    }
  }

  @objc private func handleMemoryWarning() {
    sessionQueue.async { [weak self] in
      print("CameraCapture.swift :: Received memory warning")
      // Handle memory warning if needed
    }
  }

  // MARK: - AVCapturePhotoCaptureDelegate

  func photoOutputError(errorMsg: String) {
    print("CameraCapture.swift :: \(errorMsg)")

    DispatchQueue.main.async { [weak self] in
      guard let self = self else { return }
      UnityBridge.sendMessage(
        toGameObject: self.gameObjectName,
        methodName: "OnPhotoTakenError",
        message: errorMsg
      )
    }
  }

  func photoOutput(
    _ output: AVCapturePhotoOutput,
    didFinishProcessingPhoto photo: AVCapturePhoto,
    error: Error?
  ) {
    // Reset capturing state at the end of the method
    defer {
      captureQueue.async { [weak self] in
        self?.isCapturing = false
      }
    }

    if let error = error {
      self.photoOutputError(errorMsg: "Error capturing photo: \(error.localizedDescription)")
      return
    }

    let bufferIndex = currentPhotoBufferIndex
    guard var buffer = photoBuffers[bufferIndex] else {
      print("CameraCapture.swift :: Photo buffer is nil")
      self.photoOutputError(errorMsg: "Photo buffer is nil")
      return
    }

    if photoBufferReady[bufferIndex] {
      print("CameraCapture.swift :: Photo buffer is busy")
      self.photoOutputError(errorMsg: "Photo buffer is busy")
      return
    }

    guard let photoData = photo.fileDataRepresentation() else {
      self.photoOutputError(errorMsg: "Error capturing photo: No file data representation")
      return
    }

    let dataLength = photoData.count

    // Check if buffer needs resizing
    if dataLength > photoBufferSizes[bufferIndex] {
      if !resizeBuffer(index: bufferIndex, type: .photo, requiredSize: dataLength) {
        self.photoOutputError(errorMsg: "Failed to resize photo buffer")
        return
      }
      // Re-get buffer pointer after resize
      guard let resizedBuffer = photoBuffers[bufferIndex] else {
        self.photoOutputError(errorMsg: "Photo buffer is nil after resize")
        return
      }
      buffer = resizedBuffer
    }

    // Copy data into the buffer
    photoData.copyBytes(to: buffer, count: dataLength)

    // Get photo dimensions
    guard let cgImage = photo.cgImageRepresentation() else {
      self.photoOutputError(
        errorMsg: "CameraCapture.swift :: Error getting CGImage representation of photo")
      return
    }

    let width = cgImage.width
    let height = cgImage.height

    // Update video orientation and mirroring state
    self.updateVideoOrientation()
    let imageOrientation = self.getImageOrientation()
    let isMirrored = self.currentCameraPosition == .front

    // Prepare pointer data
    let pointerData =
      "\(UInt(bitPattern: buffer)),\(width),\(height),\(dataLength),\(currentVideoOrientation.rawValue),\(imageOrientation.rawValue),\(isMirrored)"

    // Mark the buffer as ready
    photoBufferReady[bufferIndex] = true

    // Switch to the next buffer
    currentPhotoBufferIndex = (currentPhotoBufferIndex + 1) % 2

    // Send data to Unity
    DispatchQueue.main.async { [weak self] in
      guard let self = self else { return }
      UnityBridge.sendMessage(
        toGameObject: self.gameObjectName,
        methodName: "OnPhotoTaken",
        message: pointerData
      )
    }
  }

  // MARK: - AVCaptureVideoDataOutputSampleBufferDelegate

  func captureOutput(
    _ output: AVCaptureOutput,
    didOutput sampleBuffer: CMSampleBuffer,
    from connection: AVCaptureConnection
  ) {
    let currentTime = CACurrentMediaTime()
    guard currentTime - lastFrameTime >= 0.05 else { return }  // Limit to 20 fps
    guard !self.isPaused else { return }
    guard !self.isCapturing else { return }
    lastFrameTime = currentTime

    // Get the current buffer index
    let bufferIndex = currentPreviewBufferIndex
    guard var buffer = previewBuffers[bufferIndex] else {
      print("CameraCapture.swift :: Preview buffer is nil")
      return
    }

    // Ensure the buffer is not currently being read
    if previewBufferReady[bufferIndex] {
      print("CameraCapture.swift :: Preview buffer is busy, dropping frame")
      return
    }

    guard let imageBuffer = CMSampleBufferGetImageBuffer(sampleBuffer) else {
      print("CameraCapture.swift :: Failed to get image buffer")
      return
    }

    CVPixelBufferLockBaseAddress(imageBuffer, .readOnly)
    defer {
      CVPixelBufferUnlockBaseAddress(imageBuffer, .readOnly)
    }

    let width = CVPixelBufferGetWidth(imageBuffer)
    let height = CVPixelBufferGetHeight(imageBuffer)
    let bytesPerRow = CVPixelBufferGetBytesPerRow(imageBuffer)
    let dataLength = height * bytesPerRow

    // Check if buffer needs resizing
    if dataLength > previewBufferSizes[bufferIndex] {
      if !resizeBuffer(index: bufferIndex, type: .preview, requiredSize: dataLength) {
        print("CameraCapture.swift :: Failed to resize preview buffer")
        return
      }
      // Re-get buffer pointer after resize
      guard let resizedBuffer = previewBuffers[bufferIndex] else {
        print("CameraCapture.swift :: Preview buffer is nil after resize")
        return
      }
      buffer = resizedBuffer
    }

    guard let baseAddress = CVPixelBufferGetBaseAddress(imageBuffer) else {
      print("CameraCapture.swift :: Failed to get base address")
      return
    }

    // Copy data into the buffer
    memcpy(buffer, baseAddress, dataLength)

    // Update video orientation and mirroring state
    if connection.isVideoOrientationSupported {
      currentVideoOrientation = connection.videoOrientation
    }

    if connection.isVideoMirroringSupported {
      isVideoMirrored = connection.isVideoMirrored
    }

    let imageOrientation = getImageOrientation()

    // Prepare pointer data
    let pointerData =
      "\(UInt(bitPattern: buffer)),\(width),\(height),\(bytesPerRow),\(dataLength),\(currentVideoOrientation.rawValue),\(imageOrientation.rawValue),\(isVideoMirrored)"

    // Mark the buffer as ready
    previewBufferReady[bufferIndex] = true

    // Switch to the next buffer
    currentPreviewBufferIndex = (currentPreviewBufferIndex + 1) % 2

    // Send data to Unity
    DispatchQueue.main.async { [weak self] in
      guard let self = self else { return }
      UnityBridge.sendMessage(
        toGameObject: self.gameObjectName,
        methodName: "OnPreviewFrameReceived",
        message: pointerData
      )
    }
  }

  func captureOutput(
    _ output: AVCaptureOutput,
    didDrop sampleBuffer: CMSampleBuffer,
    from connection: AVCaptureConnection
  ) {
    // Handle dropped frames if needed
    print("CameraCapture.swift :: Dropped frame")
  }

  // MARK: - Buffer Management

  @objc func markPreviewBufferAsRead(_ pointer: UnsafeMutableRawPointer) {
    for i in 0..<previewBuffers.count {
      if previewBuffers[i]! == pointer {
        previewBufferReady[i] = false
        break
      }
    }
  }

  @objc func markPhotoBufferAsRead(_ pointer: UnsafeMutableRawPointer) {
    for i in 0..<photoBuffers.count {
      if photoBuffers[i]! == pointer {
        photoBufferReady[i] = false
        break
      }
    }
  }

  // MARK: - Utility Methods

  private func calculateMaxPreviewBufferSize() -> Int {
    // Calculate the maximum buffer size based on expected maximum resolution
    // For example, assume maximum resolution of 1920x1080 with 4 bytes per pixel
    let width = 1920
    let height = 1080
    let bytesPerPixel = 4
    return width * height * bytesPerPixel
  }

  private func calculateMaxPhotoBufferSize() -> Int {
    // Estimate the maximum photo size
    // For example, 10 MB
    return 10 * 1024 * 1024  // 10 MB
  }

  private func getImageOrientation() -> UIImage.Orientation {
    let isMirrored = currentCameraPosition == .front

    switch currentVideoOrientation {
    case .portrait:
      return isMirrored ? .leftMirrored : .right
    case .portraitUpsideDown:
      return isMirrored ? .rightMirrored : .left
    case .landscapeRight:
      return isMirrored ? .downMirrored : .up
    case .landscapeLeft:
      return isMirrored ? .upMirrored : .down
    @unknown default:
      return isMirrored ? .leftMirrored : .right
    }
  }

  private func normalizeGains(
    _ gains: AVCaptureDevice.WhiteBalanceGains,
    for device: AVCaptureDevice
  ) -> AVCaptureDevice.WhiteBalanceGains {
    var normalizedGains = gains
    let maxGain = device.maxWhiteBalanceGain
    normalizedGains.redGain = min(max(normalizedGains.redGain, 1.0), maxGain)
    normalizedGains.greenGain = min(max(normalizedGains.greenGain, 1.0), maxGain)
    normalizedGains.blueGain = min(max(normalizedGains.blueGain, 1.0), maxGain)
    return normalizedGains
  }
}

// MARK: - Extensions

extension AVCaptureVideoOrientation {
  init(deviceOrientation: UIDeviceOrientation) {
    switch deviceOrientation {
    case .portrait:
      self = .portrait
    case .portraitUpsideDown:
      self = .portraitUpsideDown
    case .landscapeLeft:
      self = .landscapeRight
    case .landscapeRight:
      self = .landscapeLeft
    default:
      self = .portrait
    }
  }
}

// MARK: - Error Handling

extension CameraCapture {
  enum CameraCaptureError: Error {
    case deviceNotAvailable
    case invalidInput
    case invalidOutput
    case configurationFailed
    case accessDenied

    var localizedDescription: String {
      switch self {
      case .deviceNotAvailable:
        return "Camera device is not available"
      case .invalidInput:
        return "Failed to create camera input"
      case .invalidOutput:
        return "Failed to create camera output"
      case .configurationFailed:
        return "Camera configuration failed"
      case .accessDenied:
        return "Camera access denied"
      }
    }
  }

  private func handleError(_ error: Error) {
    print("CameraCapture.swift :: Error: \(error.localizedDescription)")
    DispatchQueue.main.async { [weak self] in
      guard let self = self else { return }
      UnityBridge.sendMessage(
        toGameObject: self.gameObjectName,
        methodName: "OnCameraError",
        message: error.localizedDescription
      )
    }
  }

  private func resizeBuffer(index: Int, type: BufferType, requiredSize: Int) -> Bool {
    bufferResizeLock.sync {
      let currentSize = type == .preview ? previewBufferSizes[index] : photoBufferSizes[index]
      let newSize = calculateNewBufferSize(currentSize: currentSize, requiredSize: requiredSize)

      // Check if new size exceeds maximum
      guard newSize <= maxBufferSize else {
        print(
          "CameraCapture.swift :: Required buffer size \(requiredSize) exceeds maximum allowed size"
        )
        return false
      }

      // Keep track of old buffer
      let oldBuffer: UnsafeMutablePointer<UInt8>?
      if type == .preview {
        oldBuffer = previewBuffers[index]
      } else {
        oldBuffer = photoBuffers[index]
      }

      // Attempt to allocate new buffer
      let newBuffer: UnsafeMutablePointer<UInt8>?
      do {
        newBuffer = UnsafeMutablePointer<UInt8>.allocate(capacity: newSize)
      } catch {
        print("CameraCapture.swift :: Failed to allocate new buffer of size \(newSize)")
        return false
      }

      // Copy existing data if there's an old buffer
      if let existingBuffer = oldBuffer {
        let copySize = min(currentSize, newSize)
        newBuffer?.assign(from: existingBuffer, count: copySize)
      }

      // Update state with new buffer
      if type == .preview {
        // Deallocate old buffer before assigning new one
        previewBuffers[index]?.deallocate()
        previewBuffers[index] = newBuffer
        previewBufferSizes[index] = newSize
        previewBufferReady[index] = false
      } else {
        // Deallocate old buffer before assigning new one
        photoBuffers[index]?.deallocate()
        photoBuffers[index] = newBuffer
        photoBufferSizes[index] = newSize
        photoBufferReady[index] = false
      }

      print(
        "CameraCapture.swift :: Successfully resized \(type) buffer \(index) from \(currentSize) to \(newSize) bytes"
      )
      return true
    }
  }

  // Helper method to safely deallocate a buffer
  private func safelyDeallocateBuffer(_ buffer: inout UnsafeMutablePointer<UInt8>?) {
    if let ptr = buffer {
      ptr.deallocate()
      buffer = nil
    }
  }

  private func calculateNewBufferSize(currentSize: Int, requiredSize: Int) -> Int {
    // Calculate new size with some padding for future growth
    let growthFactor = 1.5
    let newSize = max(Int(Double(requiredSize) * growthFactor), minBufferSize)
    // Round up to nearest MB for efficiency
    let megabyte = 1024 * 1024
    return ((newSize + megabyte - 1) / megabyte) * megabyte
  }

  private enum BufferType {
    case preview
    case photo
  }
}

// MARK: - C Functions for Bridging

@_cdecl("_InitializeCamera")
public func _InitializeCamera(_ gameObjectName: UnsafePointer<CChar>) {
  let gameObjectNameSwift = String(cString: gameObjectName)
  CameraCapture.shared.initializeCamera(gameObjectName: gameObjectNameSwift)
}

@_cdecl("_StartPreview")
public func _StartPreview() {
  CameraCapture.shared.startPreview()
}

@_cdecl("_PausePreview")
public func _PausePreview() {
  CameraCapture.shared.pausePreview()
}

@_cdecl("_ResumePreview")
public func _ResumePreview() {
  CameraCapture.shared.resumePreview()
}

@_cdecl("_TakePhoto")
public func _TakePhoto() {
  CameraCapture.shared.takePhoto()
}

@_cdecl("_SwitchCamera")
public func _SwitchCamera() {
  CameraCapture.shared.switchCamera()
}

@_cdecl("_SetWhiteBalanceMode")
public func _SetWhiteBalanceMode(_ mode: Int) {
  CameraCapture.shared.setWhiteBalanceMode(mode)
}

@_cdecl("_SetColorTemperature")
public func _SetColorTemperature(_ temperature: Float) {
  CameraCapture.shared.setColorTemperature(temperature)
}

@_cdecl("_StopCamera")
public func _StopCamera() {
  CameraCapture.shared.stopCamera()
}

@_cdecl("_MarkPreviewBufferAsRead")
public func _MarkPreviewBufferAsRead(_ pointer: UnsafeMutableRawPointer) {
  CameraCapture.shared.markPreviewBufferAsRead(pointer)
}

@_cdecl("_MarkPhotoBufferAsRead")
public func _MarkPhotoBufferAsRead(_ pointer: UnsafeMutableRawPointer) {
  CameraCapture.shared.markPhotoBufferAsRead(pointer)
}
