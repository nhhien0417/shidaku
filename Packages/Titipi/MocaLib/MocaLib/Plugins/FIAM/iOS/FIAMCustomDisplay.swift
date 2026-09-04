import Foundation
import FirebaseInAppMessaging

@_silgen_name("UnitySendMessage")
func UnitySendMessage(_ obj: UnsafePointer<CChar>, _ method: UnsafePointer<CChar>, _ msg: UnsafePointer<CChar>)

final class FIAMCustomDisplay: NSObject, InAppMessagingDisplay {

    private static var pendingDelegate: InAppMessagingDisplayDelegate?
    private static var pendingMessage: InAppMessagingDisplayMessage?

    public func displayMessage(_ message: InAppMessagingDisplayMessage,
                               displayDelegate delegate: InAppMessagingDisplayDelegate) {

        var dict = [String: String]()

        // Add CampaignName if available
        let campaignName = message.campaignInfo.campaignName
        if !campaignName.isEmpty {
            dict["CustomData__CampaignName"] = campaignName
        }

        if let modal = message as? InAppMessagingModalDisplay {
            dict["Type"] = "Modal"
            dict["Title"] = modal.title
            dict["Body"] = modal.bodyText
            dict["ButtonText"] = modal.actionButton?.buttonText ?? ""

            if let custom = modal.appData {
                for (key, value) in custom {
                    if let keyStr = key as? String, let valStr = value as? String {
                        dict["CustomData__\(keyStr)"] = valStr
                    }
                }
            }

        } else if let image = message as? InAppMessagingImageOnlyDisplay {
            dict["Type"] = "Image"
            dict["CustomData__ImageUrl"] = image.imageData.imageURL

            if let custom = image.appData {
                for (key, value) in custom {
                    if let keyStr = key as? String, let valStr = value as? String {
                        dict["CustomData__\(keyStr)"] = valStr
                    }
                }
            }
        } else if let banner = message as? InAppMessagingBannerDisplay {
            dict["Type"] = "Banner"

            if let custom = banner.appData {
                for (key, value) in custom {
                    if let keyStr = key as? String, let valStr = value as? String {
                        dict["CustomData__\(keyStr)"] = valStr
                    }
                }
            }

        } else {
            print("FIAMCustomDisplay: Unsupported message type")
            return
        }

        // Convert to JSON string and send to Unity
        let jsonData = try? JSONSerialization.data(withJSONObject: dict)
        let json = String(data: jsonData ?? Data(), encoding: .utf8) ?? "{}"

        FIAMCustomDisplay.pendingMessage = message
        FIAMCustomDisplay.pendingDelegate = delegate

        UnitySendMessage("FIAMManager", "OnMessageReceived", json)

        delegate.impressionDetected?(for: message)
    }

    @objc public func onOk() {
        guard let msg = FIAMCustomDisplay.pendingMessage,
              let del = FIAMCustomDisplay.pendingDelegate else { return }

        del.messageClicked?(msg, with: InAppMessagingAction(actionText: "", actionURL: nil))

        FIAMCustomDisplay.pendingMessage = nil
        FIAMCustomDisplay.pendingDelegate = nil
    }

    @objc public func onClose() {
        guard let msg = FIAMCustomDisplay.pendingMessage,
              let del = FIAMCustomDisplay.pendingDelegate else { return }

        del.messageDismissed?(msg, dismissType: .typeUserTapClose)

        FIAMCustomDisplay.pendingMessage = nil
        FIAMCustomDisplay.pendingDelegate = nil
    }
}
