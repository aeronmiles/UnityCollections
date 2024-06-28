import AVFoundation
import UIKit

@objc
class CameraCapture: NSObject, AVCapturePhotoCaptureDelegate, AVCaptureVideoDataOutputSampleBufferDelegate {
    private var captureSession: AVCaptureSession?
    private var photoOutput: AVCapturePhotoOutput?
    private var videoDataOutput: AVCaptureVideoDataOutput?
    private var previewLayer: AVCaptureVideoPreviewLayer?
    private var currentCameraPosition: AVCaptureDevice.Position = .back
    private var lastFrameTime: TimeInterval = 0
    private var photoDataDict: [String: NSNumber] = [:]
    private var gameObjectName: String?

    static let shared = CameraCapture()

    private override init() {
        super.init()
    }

    @objc func initializeCamera(gameObjectName: String) {
        self.gameObjectName = gameObjectName
        setupCaptureSession()
    }

    private func setupCaptureSession() {
        captureSession = AVCaptureSession()
        captureSession?.beginConfiguration()

        if captureSession?.canSetSessionPreset(.photo) == true {
            captureSession?.sessionPreset = .photo
        }

        guard let camera = AVCaptureDevice.default(.builtInWideAngleCamera, for: .video, position: .back),
              let input = try? AVCaptureDeviceInput(device: camera),
              captureSession?.canAddInput(input) == true else {
            print("CameraCapture :: Failed to create camera input")
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
        if let videoDataOutput = videoDataOutput, captureSession?.canAddOutput(videoDataOutput) == true {
            captureSession?.addOutput(videoDataOutput)
        }

        previewLayer = AVCaptureVideoPreviewLayer(session: captureSession!)

        captureSession?.commitConfiguration()
    }

    @objc func startPreview() {
        DispatchQueue.global(qos: .userInitiated).async { [weak self] in
            self?.captureSession?.startRunning()
        }
    }

    @objc func takePhoto() {
        guard let photoOutput = photoOutput else { return }

        let settings = AVCapturePhotoSettings()
        settings.flashMode = .auto
        settings.isHighResolutionPhotoEnabled = true

        photoOutput.capturePhoto(with: settings, delegate: self)
    }

    @objc func switchCamera() {
        guard let captureSession = captureSession else { return }

        captureSession.beginConfiguration()

        guard let currentInput = captureSession.inputs.first as? AVCaptureDeviceInput else { return }
        captureSession.removeInput(currentInput)

        currentCameraPosition = currentCameraPosition == .back ? .front : .back

        guard let newCamera = AVCaptureDevice.default(
                .builtInWideAngleCamera, for: .video, position: currentCameraPosition),
              let newInput = try? AVCaptureDeviceInput(device: newCamera) else { return }

        captureSession.addInput(newInput)
        captureSession.commitConfiguration()
    }

    @objc func setColorTemperature(_ temperature: Float) {
        guard let device = AVCaptureDevice.default(
                .builtInWideAngleCamera, for: .video, position: currentCameraPosition) else { return }

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
        captureSession?.stopRunning()
    }

    @objc func updateOrientation(_ orientation: Int) {
        guard let connection = previewLayer?.connection, connection.isVideoOrientationSupported else {
            return
        }

        let videoOrientation: AVCaptureVideoOrientation
        switch orientation {
        case 1: videoOrientation = .portrait
        case 2: videoOrientation = .portraitUpsideDown
        case 3: videoOrientation = .landscapeRight
        case 4: videoOrientation = .landscapeLeft
        default: videoOrientation = .portrait
        }

        connection.videoOrientation = videoOrientation

        if let photoConnection = photoOutput?.connection(with: .video),
           photoConnection.isVideoOrientationSupported {
            photoConnection.videoOrientation = videoOrientation
        }
    }

    @objc func getCameraOrientationAndMirrored() -> String {
        guard let connection = previewLayer?.connection else {
            return "Unknown, false"
        }

        let orientation: String
        switch connection.videoOrientation {
        case .portrait:
            orientation = "Portrait"
        case .portraitUpsideDown:
            orientation = "PortraitUpsideDown"
        case .landscapeRight:
            orientation = "LandscapeRight"
        case .landscapeLeft:
            orientation = "LandscapeLeft"
        default:
            orientation = "Unknown"
        }

        let isMirrored = connection.isVideoMirrored
        return "\(orientation), \(isMirrored)"
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

        let pointerData = "\(UInt(bitPattern: pointer)),\(photoData.count)"
        print("CameraCapture :: Sending photo data: \(pointerData)")

        DispatchQueue.main.async {
            UnityBridge.sendMessage(toGameObject: self.gameObjectName, methodName: "OnPhotoTaken", message: pointerData)
        }
    }

    // MARK: - AVCaptureVideoDataOutputSampleBufferDelegate

    func captureOutput(
        _ output: AVCaptureOutput,
        didOutput sampleBuffer: CMSampleBuffer,
        from connection: AVCaptureConnection
    ) {
        let currentTime = CACurrentMediaTime()
        guard currentTime - lastFrameTime >= 0.1 else { return }  // Limit to 10 fps
        lastFrameTime = currentTime

        guard let imageBuffer = CMSampleBufferGetImageBuffer(sampleBuffer) else { return }

        CVPixelBufferLockBaseAddress(imageBuffer, .readOnly)
        defer { CVPixelBufferUnlockBaseAddress(imageBuffer, .readOnly) }

        let width = CVPixelBufferGetWidth(imageBuffer)
        let height = CVPixelBufferGetHeight(imageBuffer)
        let baseAddress = CVPixelBufferGetBaseAddress(imageBuffer)
        let bytesPerRow = CVPixelBufferGetBytesPerRow(imageBuffer)
        let dataLength = height * bytesPerRow

        let pointerData = "\(UInt(bitPattern: baseAddress)),\(width),\(height),\(bytesPerRow),\(dataLength)"

        DispatchQueue.main.async {
            UnityBridge.sendMessage(toGameObject: self.gameObjectName, methodName: "OnPreviewFrameReceived", message: pointerData)
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

@_cdecl("_UpdateOrientation")
public func _UpdateOrientation(_ orientation: Int32) {
    CameraCapture.shared.updateOrientation(Int(orientation))
}

@_cdecl("_GetCameraOrientationAndMirrored")
public func _GetCameraOrientationAndMirrored() -> UnsafePointer<CChar> {
    let result = CameraCapture.shared.getCameraOrientationAndMirrored()
    return UnsafePointer(strdup(result))!
}