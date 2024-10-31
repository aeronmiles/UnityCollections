import AVFoundation
import UIKit

@objc
class CameraCapture: NSObject, AVCapturePhotoCaptureDelegate,
  AVCaptureVideoDataOutputSampleBufferDelegate
{
  private var captureSession: AVCaptureSession?
  private var photoOutput: AVCapturePhotoOutput?
  private var videoDataOutput: AVCaptureVideoDataOutput?
  private var previewLayer: AVCaptureVideoPreviewLayer?
  private var currentCameraPosition: AVCaptureDevice.Position = .back
  private var lastFrameTime: TimeInterval = 0
  private var photoDataDict: [String: NSNumber] = [:]
  private var gameObjectName: String?
  private var currentVideoOrientation: AVCaptureVideoOrientation = .portrait
  private var isVideoMirrored: Bool = false
  private var isPaused: Bool = false

  // Thread safety
  private let sessionQueue = DispatchQueue(label: "com.camera.sessionQueue")
  private let photoDataLock = NSLock()
  // Add new properties for capture state management
  private let captureQueue = DispatchQueue(label: "com.camera.captureQueue")
  private let videoProcessingQueue = DispatchQueue(label: "com.camera.videoProcessingQueue")
  private var isCapturing = false
  private var lastCaptureTime: TimeInterval = 0
  private let minimumCaptureInterval: TimeInterval = 0.5  // 500ms minimum between captures
  // Camera settings
  private var clampedTemperature: Float = 2500
  private var whiteBalanceMode: AVCaptureDevice.WhiteBalanceMode = .continuousAutoWhiteBalance

  // Resource tracking
  private var currentCameraInput: AVCaptureDeviceInput?
  private var isConfiguring = false

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

      // // Check if enough time has passed since last capture
      // let currentTime = CACurrentMediaTime()
      // guard currentTime - self.lastCaptureTime >= self.minimumCaptureInterval else {
      //   print("CameraCapture.swift :: Please wait before taking another photo")
      //   self.photoOutputError(errorMsg: "Please wait before taking another photo")
      //   return
      // }

      // Set capturing state and update last capture time
      self.isCapturing = true
      // self.lastCaptureTime = currentTime

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
      settings.flashMode = .on
      settings.isHighResolutionPhotoEnabled = true


      // // Ensure white balance is set before capturing the photo
      // if let device = AVCaptureDevice.default(
      //   .builtInWideAngleCamera,
      //   for: .video,
      //   position: self.currentCameraPosition
      // ) {
      //   do {
      //     try self.configureWhiteBalance(device: device, temperature: self.clampedTemperature)
      //     print(
      //       "CameraCapture.swift :: Set color temperature to \(self.clampedTemperature)K for photo and video capture."
      //     )
      //   } catch {
      //     print(
      //       "CameraCapture.swift :: Error setting color temperature: \(error.localizedDescription)")
      //   }
      // }

      // Remove the sessionQueue dispatch if it's not necessary
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
        try self.configureWhiteBalance(device: device, temperature: clampedTemperature)
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

  @objc func freePhotoData(_ pointer: UnsafeMutableRawPointer) {
    photoDataLock.lock()
    defer { photoDataLock.unlock() }

    let address = UInt(bitPattern: pointer)
    // print(
    //   "CameraCapture.swift :: freePhotoData :: bufferData : addr : 0x\(String(address, radix: 16))")

    let key = String(format: "%lu", address)

    if let dataLengthNumber = photoDataDict.removeValue(forKey: key) {
      let dataLength = dataLengthNumber.intValue
      let typedPointer = pointer.bindMemory(to: UInt8.self, capacity: dataLength)
      typedPointer.deallocate()
    } else {
      print("CameraCapture.swift :: Error: Attempted to free unknown pointer")
    }
  }

  private func cleanup() {
    captureSession?.stopRunning()

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

    // Clear photo data dictionary
    photoDataLock.lock()
    photoDataDict.removeAll()
    photoDataLock.unlock()

    // Clear references
    captureSession = nil
    photoOutput = nil
    videoDataOutput = nil
    currentCameraInput = nil
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
        position: .back)
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
      videoDataOutput.setSampleBufferDelegate(self, queue: DispatchQueue.main)

      if captureSession.canAddOutput(videoDataOutput) {
        captureSession.addOutput(videoDataOutput)
      }
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
      // Clean up any cached data
      // self?.photoDataLock.lock()
      // self?.photoDataDict.removeAll()
      // self?.photoDataLock.unlock()
    }
  }

  // MARK: - AVCapturePhotoCaptureDelegate

  // Update error handling method
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

  // Add method to check capture state
  @objc func isPhotoCapturing() -> Bool {
    var capturing = false
    captureQueue.sync {
      capturing = self.isCapturing
    }
    return capturing
  }

  // Update photo output delegate method
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
      self.photoOutputError(
        errorMsg: "CameraCapture.swift :: Error capturing photo: \(error.localizedDescription)")
      return
    }

    guard let photoData = photo.fileDataRepresentation() else {
      self.photoOutputError(
        errorMsg: "CameraCapture.swift :: Error capturing photo: No file data representation")
      return
    }
    let pointer = UnsafeMutablePointer<UInt8>.allocate(capacity: photoData.count)
    photoData.copyBytes(to: pointer, count: photoData.count)
    // print pointer hexadecimal address
    // print("CameraCapture.swift :: photoOutput :: pointer : addr : \(String(format: "%p", pointer))")

    photoDataLock.lock()
    let key = String(format: "%lu", UInt(bitPattern: pointer))
    photoDataDict[key] = NSNumber(value: photoData.count)
    photoDataLock.unlock()

    guard let cgImage = photo.cgImageRepresentation() else {
      freePhotoData(pointer)
      self.photoOutputError(
        errorMsg: "CameraCapture.swift :: Error getting CGImage representation of photo")
      return
    }

    let width = cgImage.width
    let height = cgImage.height

    var isMirrored = false
    if let connection = output.connection(with: .video) {
      isMirrored = connection.isVideoMirrored
    }

    let imageOrientation = getImageOrientation()
    let pointerData =
      "\(UInt(bitPattern: pointer)),\(width),\(height),\(photoData.count),\(currentVideoOrientation.rawValue),\(imageOrientation.rawValue),\(isMirrored)"

    DispatchQueue.main.async { [weak self] in
      guard let self = self else {
        self?.freePhotoData(pointer)
        return
      }

      UnityBridge.sendMessage(
        toGameObject: self.gameObjectName,
        methodName: "OnPhotoTaken",
        message: pointerData
      )
    }
  }

  // MARK: - AVCaptureVideoDataOutputSampleBufferDelegate (continued)
  func captureOutput(
    _ output: AVCaptureOutput,
    didOutput sampleBuffer: CMSampleBuffer,
    from connection: AVCaptureConnection
  ) {
    let currentTime = CACurrentMediaTime()
    guard currentTime - lastFrameTime >= 0.05 else { return }  // Limit to 10 fps
    guard !self.isPaused else { return }
    guard !self.isCapturing else { return }
    lastFrameTime = currentTime

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

    guard let baseAddress = CVPixelBufferGetBaseAddress(imageBuffer) else {
      print("CameraCapture.swift :: Failed to get base address")
      return
    }

    // Create Data object from buffer - this creates a copy of the data
    let bufferData = Data(bytes: baseAddress, count: dataLength)
    // Allocate new memory and copy the frame data
    let pointer = UnsafeMutablePointer<UInt8>.allocate(capacity: dataLength)
    bufferData.copyBytes(to: pointer, count: dataLength)

    // print pointer hexadecimal address
    // print(
    //   "CameraCapture.swift :: catureOutput :: pointer : addr : \(String(format: "%p", pointer))")

    // Store the data length in the dictionary for cleanup
    photoDataLock.lock()
    let key = String(format: "%lu", UInt(bitPattern: pointer))
    photoDataDict[key] = NSNumber(value: dataLength)
    photoDataLock.unlock()

    // Update video orientation and mirroring state
    if connection.isVideoOrientationSupported {
      currentVideoOrientation = connection.videoOrientation
    }

    if connection.isVideoMirroringSupported {
      isVideoMirrored = connection.isVideoMirrored
    }

    let imageOrientation = getImageOrientation()

    let pointerData =
      "\(UInt(bitPattern: pointer)),\(width),\(height),\(bytesPerRow),\(dataLength),\(currentVideoOrientation.rawValue),\(imageOrientation.rawValue),\(isVideoMirrored)"

    // Capture weak self and pointer for cleanup in case of failure
    weak var weakSelf = self
    DispatchQueue.main.async {
      // Check if we're paused first
      if weakSelf?.isPaused == true {
        weakSelf?.freePhotoData(pointer)
        return
      }

      // Ensure we still have a valid self and gameObjectName
      guard let strongSelf = weakSelf,
        let gameObjectName = strongSelf.gameObjectName
      else {
        // If self is deallocated, ensure we free the pointer
        weakSelf?.freePhotoData(pointer)
        return
      }

      // Send the frame data to Unity
      UnityBridge.sendMessage(
        toGameObject: gameObjectName,
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
    // Log dropped frames for debugging performance issues
    print("CameraCapture.swift :: Dropped frame")
  }

  // MARK: - Utility Methods

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
    normalizedGains.redGain = min(normalizedGains.redGain, maxGain)
    normalizedGains.greenGain = min(normalizedGains.greenGain, maxGain)
    normalizedGains.blueGain = min(normalizedGains.blueGain, maxGain)
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
}

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

@_cdecl("_FreePhotoData")
public func _FreePhotoData(_ pointer: UnsafeMutableRawPointer) {
  CameraCapture.shared.freePhotoData(pointer)
}