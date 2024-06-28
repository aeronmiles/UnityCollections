#include <stdio.h>

#if defined(__CYGWIN32__)
    #define UNITY_INTERFACE_API __stdcall
    #define UNITY_INTERFACE_EXPORT __declspec(dllexport)
#elif defined(WIN32) || defined(_WIN32) || defined(__WIN32__) || defined(_WIN64) || defined(WINAPI_FAMILY)
    #define UNITY_INTERFACE_API __stdcall
    #define UNITY_INTERFACE_EXPORT __declspec(dllexport)
#elif defined(__MACH__) || defined(__ANDROID__) || defined(__linux__)
    #define UNITY_INTERFACE_API
    #define UNITY_INTERFACE_EXPORT
#else
    #define UNITY_INTERFACE_API
    #define UNITY_INTERFACE_EXPORT
#endif

extern void UnitySendMessage(const char* obj, const char* method, const char* msg);

UNITY_INTERFACE_EXPORT void UNITY_INTERFACE_API UnitySendMessageToMethod(const char* objectName, const char* methodName, const char* message) {
    UnitySendMessage(objectName, methodName, message);
}