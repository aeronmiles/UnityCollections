import Foundation

@objc public class UnityBridge: NSObject {
    @objc public static func setup() {
        print("UnityBridge setup completed")
        // Add any necessary initialization code here
    }
    
    @objc public static func sendMessage(toGameObject gameObject: String?, methodName: String, message: String) {
        guard let gameObject = gameObject else { return }
        _unitySendMessage(gameObject, methodName, message)
    }
    
    private static func _unitySendMessage(_ gameObject: String, _ methodName: String, _ message: String) {
        gameObject.withCString { goPtr in
            methodName.withCString { methodPtr in
                message.withCString { msgPtr in
                    __UnitySendMessage(goPtr, methodPtr, msgPtr)
                }
            }
        }
    }
}

@_cdecl("UnityBridge_setup")
public func UnityBridge_setup() {
    UnityBridge.setup()
}

@_cdecl("UnityBridge_sendMessage")
public func UnityBridge_sendMessage(_ gameObject: UnsafePointer<Int8>?, _ methodName: UnsafePointer<Int8>?, _ message: UnsafePointer<Int8>?) {
    let gameObjectSwift = gameObject.map { String(cString: $0) }
    let methodNameSwift = methodName.map { String(cString: $0) } ?? ""
    let messageSwift = message.map { String(cString: $0) } ?? ""
    
    UnityBridge.sendMessage(toGameObject: gameObjectSwift, methodName: methodNameSwift, message: messageSwift)
}

// This declaration tells Swift that this function will be provided by the Unity runtime
@_silgen_name("UnitySendMessage")
private func __UnitySendMessage(_ objectName: UnsafePointer<Int8>?, _ methodName: UnsafePointer<Int8>?, _ message: UnsafePointer<Int8>?)