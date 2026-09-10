#import <AuthenticationServices/AuthenticationServices.h>
#import <UIKit/UIKit.h>

extern void UnitySendMessage(const char *obj, const char *method, const char *msg);

@interface PGEAppleSignInDelegate : NSObject<ASAuthorizationControllerDelegate, ASAuthorizationControllerPresentationContextProviding>
@property(nonatomic, copy) NSString *target;
@end

@implementation PGEAppleSignInDelegate
- (ASPresentationAnchor)presentationAnchorForAuthorizationController:(ASAuthorizationController *)controller
{
    return UIApplication.sharedApplication.keyWindow;
}
- (void)authorizationController:(ASAuthorizationController *)controller didCompleteWithAuthorization:(ASAuthorization *)authorization
{
    ASAuthorizationAppleIDCredential *credential = (ASAuthorizationAppleIDCredential *)authorization.credential;
    NSString *token = [[NSString alloc] initWithData:credential.identityToken encoding:NSUTF8StringEncoding];
    NSString *payload = token.length > 0 ? [@"success|" stringByAppendingString:token] : @"error|Apple returned an empty identity token.";
    UnitySendMessage(self.target.UTF8String, "OnAppleNativeResult", payload.UTF8String);
}
- (void)authorizationController:(ASAuthorizationController *)controller didCompleteWithError:(NSError *)error
{
    NSString *message = error.code == ASAuthorizationErrorCanceled ? @"Apple Sign In was cancelled." : @"Apple Sign In failed.";
    NSString *payload = [@"error|" stringByAppendingString:message];
    UnitySendMessage(self.target.UTF8String, "OnAppleNativeResult", payload.UTF8String);
}
@end

static PGEAppleSignInDelegate *pgeAppleDelegate;

extern "C" void PGE_StartAppleSignIn(const char *gameObjectName)
{
    if (@available(iOS 13.0, *)) {
        pgeAppleDelegate = [PGEAppleSignInDelegate new];
        pgeAppleDelegate.target = [NSString stringWithUTF8String:gameObjectName];
        ASAuthorizationAppleIDProvider *provider = [ASAuthorizationAppleIDProvider new];
        ASAuthorizationAppleIDRequest *request = provider.createRequest;
        request.requestedScopes = @[ASAuthorizationScopeFullName, ASAuthorizationScopeEmail];
        ASAuthorizationController *controller = [[ASAuthorizationController alloc] initWithAuthorizationRequests:@[request]];
        controller.delegate = pgeAppleDelegate;
        controller.presentationContextProvider = pgeAppleDelegate;
        [controller performRequests];
    } else {
        UnitySendMessage(gameObjectName, "OnAppleNativeResult", "error|Sign in with Apple requires iOS 13 or newer.");
    }
}
