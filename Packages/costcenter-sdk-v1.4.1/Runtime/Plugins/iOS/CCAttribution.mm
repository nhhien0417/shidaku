#import <AdServices/AdServices.h>
#import <AdSupport/ASIdentifierManager.h>
#import <AppTrackingTransparency/AppTrackingTransparency.h>
// #import <AdSupport/AdSupport.h>


char* CCMakeStringCopy (const char* string)
{
    if (string == NULL)
        return NULL;
    
    char* res = (char*)malloc(strlen(string) + 1);
    strcpy(res, string);
    return res;
}

extern "C" {
	
	const char* _CCGetAttributionToken()
	{
        if (@available(iOS 14.3, *)) {
            NSError *error;
            NSString *attributionToken = [AAAttribution attributionTokenWithError:&error];
            return CCMakeStringCopy([attributionToken UTF8String]);
        }
        return NULL;
	}

    const char* _CCGetIDFA()
	{
        if (@available(iOS 14, *)) {
            NSString *idfaString = [[[ASIdentifierManager sharedManager] advertisingIdentifier] UUIDString];
            return CCMakeStringCopy([idfaString UTF8String]);
        }
        return NULL;
	}

    // - (const char*)getAttributionToken {
    //     @try {
    //         NSError *error;
    //         NSString *attributionToken = [AAAttribution attributionTokenWithError:&error];
            
    //         NSLog(@"Attribution Token: %@", attributionToken);
    //         return [attributionToken UTF8String];
    //     } @catch (NSException *exception) {
    //         return NULL;
    //     }
    // }

    // bool _CCIsATTTrackingEnabled() {
    //     if (@available(iOS 14, *)) {
    //         ATTrackingManagerAuthorizationStatus trackingAuthorizationStatus = [ATTrackingManager trackingAuthorizationStatus];
    //         NSLog(@"%lu", trackingAuthorizationStatus);
    //         if (trackingAuthorizationStatus == ATTrackingManagerAuthorizationStatusAuthorized) {
    //             NSLog(@"Advertising tracking is enabled");
    //             return TRUE;
    //         } else {
    //             NSLog(@"Advertising tracking is not enabled");
    //         }
    //     } else {
    //         // Fallback on earlier versions
    //         NSLog(@"Advertising tracking status not available on this iOS version");
    //     }
    //     return FALSE;
    // }

    void _CCRequestTrackingAuthorization() {
        if (@available(iOS 14, *)) {
            [ATTrackingManager requestTrackingAuthorizationWithCompletionHandler:^(ATTrackingManagerAuthorizationStatus status) {
                switch(status) {
                    case ATTrackingManagerAuthorizationStatusNotDetermined:
                        NSLog(@"Unknown consent");
                        break;
                    case ATTrackingManagerAuthorizationStatusRestricted:
                        NSLog(@"Device has an MDM solution applied");
                        break;
                    case ATTrackingManagerAuthorizationStatusDenied:
                        NSLog(@"Denied consent");
                        break;
                    case ATTrackingManagerAuthorizationStatusAuthorized:
                        NSLog(@"Granted consent");
                        break;
                    default:
                        NSLog(@"Unknown");
                        break;
                }
                // callback(_CCGetIDFA());
            }];
        } else {
            // Fallback on earlier versions
            NSLog(@"Tracking authorization not available on this iOS version");
            // callback(_CCGetIDFA());
        }
    }
	
}
