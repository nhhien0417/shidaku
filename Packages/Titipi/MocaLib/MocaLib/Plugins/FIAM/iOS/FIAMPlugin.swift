import Foundation
import FirebaseCore
import FirebaseInAppMessaging

@objc public class FIAMPlugin: NSObject {
    private static var customDisplay: FIAMCustomDisplay?

    // Renamed from `initialize()` to `pluginInitialize()`
    @objc public static func pluginInitialize() {
        guard FirebaseApp.app() != nil else {
            print("FIAMPlugin: FirebaseApp not initialized!")
            return
        }
        let display = FIAMCustomDisplay()
        InAppMessaging.inAppMessaging().messageDisplayComponent = display
        customDisplay = display
        print("FIAMPlugin: Registered custom FIAM display")
    }

    @objc public static func onOk() {
        customDisplay?.onOk()
    }

    @objc public static func onClose() {
        customDisplay?.onClose()
    }
}

// C-callable wrappers for Unity → iOS interop
@_cdecl("FIAMPlugin_Initialize")
public func FIAMPlugin_Initialize() {
    FIAMPlugin.pluginInitialize()
}

@_cdecl("FIAMPlugin_OnOk")
public func FIAMPlugin_OnOk() {
    FIAMPlugin.onOk()
}

@_cdecl("FIAMPlugin_OnClose")
public func FIAMPlugin_OnClose() {
    FIAMPlugin.onClose()
}
