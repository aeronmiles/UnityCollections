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

  static let shared = CameraCapture()

  private override init() {
    super.init()
  }

  @objc func initializeCamera(gameObjectName: String) {
    self.gameObjectName = gameObjectName
    setupCaptureSession()
  }

  private func setupCaptureSession() {
    print("CameraCapture.swift :: Setting up capture session")
    captureSession = AVCaptureSession()
    captureSession?.beginConfiguration()

    if captureSession?.canSetSessionPreset(.photo) == true {
      captureSession?.sessionPreset = .photo
    }

    guard
      let camera = AVCaptureDevice.default(.builtInWideAngleCamera, for: .video, position: .back),
      let input = try? AVCaptureDeviceInput(device: camera),
      captureSession?.canAddInput(input) == true
    else {
      print("CameraCapture.swift :: Failed to create camera input")
      return
    }

    captureSession?.addInput(input)

    photoOutput = AVCapturePhotoOutput()
    photoOutput?.isHighResolutionCaptureEnabled = true
    if let photoOutput = photoOutput, captureSession?.canAddOutput(photoOutput) == true {
      captureSession?.addOutput(photoOutput)
    }

    videoDataOutput = AVCaptureVideoDataOutput()
    videoDataOutput?.videoSettings = [
      kCVPixelBufferPixelFormatTypeKey as String: kCVPixelFormatType_32BGRA
    ]
    videoDataOutput?.alwaysDiscardsLateVideoFrames = true
    videoDataOutput?.setSampleBufferDelegate(self, queue: DispatchQueue.main)
    if let videoDataOutput = videoDataOutput, captureSession?.canAddOutput(videoDataOutput) == true
    {
      captureSession?.addOutput(videoDataOutput)
    }

    previewLayer = AVCaptureVideoPreviewLayer(session: captureSession!)

    captureSession?.commitConfiguration()

    DispatchQueue.main.async {
      UnityBridge.sendMessage(
        toGameObject: self.gameObjectName, methodName: "OnCameraInitialized",
        message: "")
    }
  }

  @objc func startPreview() {
    print("CameraCapture.swift :: Starting preview")
    DispatchQueue.global(qos: .userInitiated).async { [weak self] in
      self?.captureSession?.startRunning()
    }
  }

  @objc func pausePreview() {
    print("CameraCapture.swift :: Pausing preview")
    isPaused = true
    captureSession?.stopRunning()
    DispatchQueue.main.async {
      UnityBridge.sendMessage(
        toGameObject: self.gameObjectName, methodName: "OnPreviewPaused", message: "")
    }
  }

  @objc func resumePreview() {
    print("CameraCapture.swift :: Resuming preview")
    isPaused = false
    captureSession?.startRunning()
    DispatchQueue.main.async {
      UnityBridge.sendMessage(
        toGameObject: self.gameObjectName, methodName: "OnPreviewResumed", message: "")
    }
  }

  private func updateVideoOrientation() {
    let deviceOrientation = UIDevice.current.orientation
    switch deviceOrientation {
    case .portrait:
      currentVideoOrientation = .portrait
    case .portraitUpsideDown:
      currentVideoOrientation = .portraitUpsideDown
    case .landscapeLeft:
      currentVideoOrientation = .landscapeRight
    case .landscapeRight:
      currentVideoOrientation = .landscapeLeft
    default:
      currentVideoOrientation = .portrait
    }

    if let connection = photoOutput?.connection(with: .video) {
      if connection.isVideoOrientationSupported {
        connection.videoOrientation = currentVideoOrientation
      }
    }
  }

  @objc func takePhoto() {
    print("CameraCapture.swift :: Taking photo")
    guard let photoOutput = photoOutput else { return }

    updateVideoOrientation()

    let settings = AVCapturePhotoSettings()
    settings.flashMode = .on
    settings.isHighResolutionPhotoEnabled = true

    photoOutput.capturePhoto(with: settings, delegate: self)
  }

  @objc func switchCamera() {
    print("CameraCapture.swift :: Switching camera")
    guard let captureSession = captureSession else { return }

    captureSession.beginConfiguration()

    guard let currentInput = captureSession.inputs.first as? AVCaptureDeviceInput else { return }
    captureSession.removeInput(currentInput)

    currentCameraPosition = currentCameraPosition == .back ? .front : .back

    guard
      let newCamera = AVCaptureDevice.default(
        .builtInWideAngleCamera, for: .video, position: currentCameraPosition),
      let newInput = try? AVCaptureDeviceInput(device: newCamera)
    else { return }

    captureSession.addInput(newInput)
    captureSession.commitConfiguration()
  }

  @objc func setColorTemperature(_ temperature: Float) {
    print("CameraCapture.swift :: Setting color temperature: \(temperature)")
    guard
      let device = AVCaptureDevice.default(
        .builtInWideAngleCamera, for: .video, position: currentCameraPosition)
    else { return }

    do {
      try device.lockForConfiguration()
      if device.isWhiteBalanceModeSupported(.locked) {
        let temperatureAndTint = AVCaptureDevice.WhiteBalanceTemperatureAndTintValues(
          temperature: temperature, tint: 0)
        let gains = device.deviceWhiteBalanceGains(for: temperatureAndTint)
        let normalizedGains = normalizeGains(gains, for: device)
        device.setWhiteBalanceModeLocked(with: normalizedGains, completionHandler: nil)
      }
      device.unlockForConfiguration()
    } catch {
      print("CameraCapture :: Error setting color temperature: \(error.localizedDescription)")
    }
  }

  private func normalizeGains(
    _ gains: AVCaptureDevice.WhiteBalanceGains, for device: AVCaptureDevice
  ) -> AVCaptureDevice.WhiteBalanceGains {
    var normalizedGains = gains
    let maxGain = device.maxWhiteBalanceGain
    normalizedGains.redGain = min(normalizedGains.redGain, maxGain)
    normalizedGains.greenGain = min(normalizedGains.greenGain, maxGain)
    normalizedGains.blueGain = min(normalizedGains.blueGain, maxGain)
    return normalizedGains
  }

  @objc func stopCamera() {
    print("CameraCapture.swift :: Stopping camera")
    captureSession?.stopRunning()
    DispatchQueue.main.async {
      UnityBridge.sendMessage(
        toGameObject: self.gameObjectName, methodName: "OnCameraStopped", message: "")
    }
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

  @objc func getCameraOrientation() -> String {
    let imageOrientation = getImageOrientation()
    return "\(currentVideoOrientation.rawValue),\(imageOrientation.rawValue)"
  }

  // MARK: - AVCapturePhotoCaptureDelegate

  func photoOutput(
    _ output: AVCapturePhotoOutput,
    didFinishProcessingPhoto photo: AVCapturePhoto,
    error: Error?
  ) {
    guard let photoData = photo.fileDataRepresentation() else {
      print("CameraCapture :: Error capturing photo: No file data representation")
      return
    }

    let pointer = UnsafeMutablePointer<UInt8>.allocate(capacity: photoData.count)
    photoData.copyBytes(to: pointer, count: photoData.count)

    let key = String(format: "%lu", UInt(bitPattern: pointer))
    photoDataDict[key] = NSNumber(value: photoData.count)

    let imageOrientation = getImageOrientation()

    var isMirrored = false
    if let connection = output.connection(with: .video), connection.isVideoMirroringSupported {
      isMirrored = connection.isVideoMirrored
    }

    let pointerData =
      "\(UInt(bitPattern: pointer)),\(photoData.count),\(currentVideoOrientation.rawValue),\(imageOrientation.rawValue),\(isMirrored)"
    print("CameraCapture :: Sending photo data: \(pointerData)")

    DispatchQueue.main.async {
      UnityBridge.sendMessage(
        toGameObject: self.gameObjectName, methodName: "OnPhotoTaken", message: pointerData)
    }
  }

  // MARK: - AVCaptureVideoDataOutputSampleBufferDelegate

  func captureOutput(
    _ output: AVCaptureOutput,
    didOutput sampleBuffer: CMSampleBuffer,
    from connection: AVCaptureConnection
  ) {
    let currentTime = CACurrentMediaTime()
    guard currentTime - lastFrameTime >= 0.05 else { return }  // Limit to 10 fps
    guard !isPaused else { return }
    lastFrameTime = currentTime

    guard let imageBuffer = CMSampleBufferGetImageBuffer(sampleBuffer) else { return }

    CVPixelBufferLockBaseAddress(imageBuffer, .readOnly)
    defer { CVPixelBufferUnlockBaseAddress(imageBuffer, .readOnly) }

    let width = CVPixelBufferGetWidth(imageBuffer)
    let height = CVPixelBufferGetHeight(imageBuffer)
    let baseAddress = CVPixelBufferGetBaseAddress(imageBuffer)
    let bytesPerRow = CVPixelBufferGetBytesPerRow(imageBuffer)
    let dataLength = height * bytesPerRow

    // Update video orientation based on the connection
    if connection.isVideoOrientationSupported {
      currentVideoOrientation = connection.videoOrientation
    }

    if let connection = videoDataOutput?.connection(with: .video) {
      if connection.isVideoMirroringSupported {
        isVideoMirrored = connection.isVideoMirrored
      }
    }

    let imageOrientation = getImageOrientation()

    let pointerData =
      "\(UInt(bitPattern: baseAddress)),\(width),\(height),\(bytesPerRow),\(dataLength),\(currentVideoOrientation.rawValue),\(imageOrientation.rawValue),\(isVideoMirrored)"

    DispatchQueue.main.async {
      UnityBridge.sendMessage(
        toGameObject: self.gameObjectName, methodName: "OnPreviewFrameReceived",
        message: pointerData)
    }
  }

  @objc func freePhotoData(_ pointer: UnsafeMutableRawPointer) {
    let key = String(format: "%lu", UInt(bitPattern: pointer))
    photoDataDict.removeValue(forKey: key)
    pointer.deallocate()
  }
}

// MARK: - C interface

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

@_cdecl("_GetCameraOrientation")
public func _GetCameraOrientation() -> UnsafePointer<CChar> {
  let result = CameraCapture.shared.getCameraOrientation()
  return UnsafePointer(strdup(result))
}
