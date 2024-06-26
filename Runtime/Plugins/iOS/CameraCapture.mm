// CameraCapture.mm

#import <Foundation/Foundation.h>
#import <AVFoundation/AVFoundation.h>

extern "C" {
    void UnitySendMessage(const char* obj, const char* method, const char* msg);
}

@interface CameraDelegate : NSObject <AVCapturePhotoCaptureDelegate, AVCaptureVideoDataOutputSampleBufferDelegate>
@property (nonatomic, strong) NSString* gameObjectName;
@property (nonatomic, strong) AVCaptureVideoDataOutput* videoDataOutput;
@end

@implementation CameraDelegate

- (void)captureOutput:(AVCapturePhotoOutput *)output didFinishProcessingPhoto:(AVCapturePhoto *)photo error:(NSError *)error {
    if (error) {
        NSLog(@"Error capturing photo: %@", error.localizedDescription);
        return;
    }
    
    NSData *photoData = [photo fileDataRepresentation];
    NSString *encodedPhoto = [photoData base64EncodedStringWithOptions:0];
    
    UnitySendMessage([self.gameObjectName UTF8String], "OnPhotoTaken", [encodedPhoto UTF8String]);
}

- (void)captureOutput:(AVCaptureOutput *)output didOutputSampleBuffer:(CMSampleBufferRef)sampleBuffer fromConnection:(AVCaptureConnection *)connection {
    CVImageBufferRef imageBuffer = CMSampleBufferGetImageBuffer(sampleBuffer);
    
    CVPixelBufferLockBaseAddress(imageBuffer, 0);
    
    size_t width = CVPixelBufferGetWidth(imageBuffer);
    size_t height = CVPixelBufferGetHeight(imageBuffer);
    uint8_t *baseAddress = (uint8_t *)CVPixelBufferGetBaseAddress(imageBuffer);
    size_t bytesPerRow = CVPixelBufferGetBytesPerRow(imageBuffer);
    
    NSData *data = [NSData dataWithBytes:baseAddress length:height * bytesPerRow];
    NSString *encodedData = [data base64EncodedStringWithOptions:0];
    
    CVPixelBufferUnlockBaseAddress(imageBuffer, 0);
    
    NSString *dimensions = [NSString stringWithFormat:@"%zu,%zu,%zu", width, height, bytesPerRow];
    NSString *message = [NSString stringWithFormat:@"%@|%@", dimensions, encodedData];
    
    UnitySendMessage([self.gameObjectName UTF8String], "OnPreviewFrameReceived", [message UTF8String]);
}

@end

static AVCaptureSession *captureSession;
static AVCapturePhotoOutput *photoOutput;
static CameraDelegate *delegate;
static AVCaptureDevicePosition currentCameraPosition = AVCaptureDevicePositionBack;

void InitializeCamera() {
    if (captureSession) return;
    
    captureSession = [[AVCaptureSession alloc] init];
    [captureSession beginConfiguration];
    
    if ([captureSession canSetSessionPreset:AVCaptureSessionPresetPhoto]) {
        captureSession.sessionPreset = AVCaptureSessionPresetPhoto;
    }
    
    AVCaptureDevice *camera = [AVCaptureDevice defaultDeviceWithMediaType:AVMediaTypeVideo];
    NSError *error = nil;
    AVCaptureDeviceInput *input = [AVCaptureDeviceInput deviceInputWithDevice:camera error:&error];
    
    if (!input) {
        NSLog(@"Failed to create camera input: %@", error.localizedDescription);
        return;
    }
    
    [captureSession addInput:input];
    
    photoOutput = [[AVCapturePhotoOutput alloc] init];
    
    // Enable high resolution photos
    photoOutput.highResolutionCaptureEnabled = YES;
    
    [captureSession addOutput:photoOutput];
    
    delegate = [[CameraDelegate alloc] init];
    
    AVCaptureVideoDataOutput *videoDataOutput = [[AVCaptureVideoDataOutput alloc] init];
    videoDataOutput.videoSettings = @{(id)kCVPixelBufferPixelFormatTypeKey: @(kCVPixelFormatType_32BGRA)};
    videoDataOutput.alwaysDiscardsLateVideoFrames = YES;
    [videoDataOutput setSampleBufferDelegate:delegate queue:dispatch_get_main_queue()];
    [captureSession addOutput:videoDataOutput];
    delegate.videoDataOutput = videoDataOutput;
    
    [captureSession commitConfiguration];
}

AVCaptureWhiteBalanceGains NormalizeGains(AVCaptureWhiteBalanceGains gains, AVCaptureDevice *device) {
    float maxGain = device.maxWhiteBalanceGain;
    
    gains.redGain = MIN(gains.redGain, maxGain);
    gains.greenGain = MIN(gains.greenGain, maxGain);
    gains.blueGain = MIN(gains.blueGain, maxGain);
    
    return gains;
}

void SetColorTemperature(float temperature) {
    if (!captureSession) InitializeCamera();
    
    AVCaptureDevice *device = [AVCaptureDevice defaultDeviceWithMediaType:AVMediaTypeVideo];
    if ([device lockForConfiguration:nil]) {
        if ([device isWhiteBalanceModeSupported:AVCaptureWhiteBalanceModeLocked]) {
            AVCaptureWhiteBalanceTemperatureAndTintValues temperatureAndTint = {
                .temperature = temperature,
                .tint = 0.0
            };
            
            AVCaptureWhiteBalanceGains gains = [device deviceWhiteBalanceGainsForTemperatureAndTintValues:temperatureAndTint];
            
            AVCaptureWhiteBalanceGains normalizedGains = NormalizeGains(gains, device);
            
            [device setWhiteBalanceModeLockedWithDeviceWhiteBalanceGains:normalizedGains completionHandler:nil];
        }
        [device unlockForConfiguration];
    }
}

extern "C" {
    void _InitializeCamera(const char* gameObjectName) {
        InitializeCamera();
        delegate.gameObjectName = [NSString stringWithUTF8String:gameObjectName];
    }
    
    void _StartPreview() {
        if (captureSession && ![captureSession isRunning]) {
            [captureSession startRunning];
        }
    }
    
    void _TakePhoto() {
        if (!captureSession) return;
        
        AVCapturePhotoSettings *settings = [AVCapturePhotoSettings photoSettingsWithFormat:@{
            AVVideoCodecKey: AVVideoCodecTypeJPEG,
            AVVideoCompressionPropertiesKey: @{
                AVVideoQualityKey: @1.0
            }
        }];
        
        // Enable auto flash if available
        if ([photoOutput.supportedFlashModes containsObject:@(AVCaptureFlashModeAuto)]) {
            settings.flashMode = AVCaptureFlashModeAuto;
        }
        
        // Enable high resolution capture
        settings.highResolutionPhotoEnabled = YES;
        
        [photoOutput capturePhotoWithSettings:settings delegate:delegate];
    }
    
    void _SwitchCamera() {
        [captureSession beginConfiguration];
        
        AVCaptureInput *currentInput = [captureSession.inputs firstObject];
        [captureSession removeInput:currentInput];
        
        currentCameraPosition = (currentCameraPosition == AVCaptureDevicePositionBack) ? AVCaptureDevicePositionFront : AVCaptureDevicePositionBack;
        
        AVCaptureDevice *newCamera = [AVCaptureDevice defaultDeviceWithDeviceType:AVCaptureDeviceTypeBuiltInWideAngleCamera mediaType:AVMediaTypeVideo position:currentCameraPosition];
        AVCaptureDeviceInput *newInput = [AVCaptureDeviceInput deviceInputWithDevice:newCamera error:nil];
        
        [captureSession addInput:newInput];
        
        [captureSession commitConfiguration];
    }
    
    void _SetColorTemperature(float temperature) {
        SetColorTemperature(temperature);
    }
    
    void _StopCamera() {
        if (captureSession && [captureSession isRunning]) {
            [captureSession stopRunning];
        }
    }
}