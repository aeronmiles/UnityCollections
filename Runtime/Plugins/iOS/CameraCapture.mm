// CameraCapture.mm

#import <Foundation/Foundation.h>
#import <AVFoundation/AVFoundation.h>

extern "C" {
    void UnitySendMessage(const char* obj, const char* method, const char* msg);
}

@interface CameraDelegate : NSObject <AVCapturePhotoCaptureDelegate>
@property (nonatomic, strong) NSString* gameObjectName;
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

@end

static AVCaptureSession *captureSession;
static AVCapturePhotoOutput *photoOutput;
static CameraDelegate *delegate;
static AVCaptureDevicePosition currentCameraPosition = AVCaptureDevicePositionBack;

void InitializeCamera() {
    if (captureSession) return;
    
    captureSession = [[AVCaptureSession alloc] init];
    [captureSession beginConfiguration];
    
    AVCaptureDevice *camera = [AVCaptureDevice defaultDeviceWithMediaType:AVMediaTypeVideo];
    NSError *error = nil;
    AVCaptureDeviceInput *input = [AVCaptureDeviceInput deviceInputWithDevice:camera error:&error];
    
    if (!input) {
        NSLog(@"Failed to create camera input: %@", error.localizedDescription);
        return;
    }
    
    [captureSession addInput:input];
    
    photoOutput = [[AVCapturePhotoOutput alloc] init];
    [captureSession addOutput:photoOutput];
    
    [captureSession commitConfiguration];
    [captureSession startRunning];
    
    delegate = [[CameraDelegate alloc] init];
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
    void _TakePhoto(const char* gameObjectName) {
        InitializeCamera();
        delegate.gameObjectName = [NSString stringWithUTF8String:gameObjectName];
        
        AVCapturePhotoSettings *settings = [AVCapturePhotoSettings photoSettings];
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
}