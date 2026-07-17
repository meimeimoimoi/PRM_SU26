namespace SmartDine.Domain.Constants;
/// <summary>/// T+�n c+�c custom claim d+�ng trong JWT (ngo+�i c+�c claim chuߦ�n c�+�a ClaimTypes).
/// </summary>
public static class JwtClaimTypes{    
/// /// <summary>    
/// Claim ch�+�a DiningSession.Id th�+�c tߦ+. D+�ng cho GUEST v+� claim "sub" (NameIdentifier)    
/// c�+�a GUEST l+� m�+�t UUID -��+�nh danh phi+�n -�-�ng nhߦ�p, kh+�ng phߦ�i sessionId.    
/// </summary>    
public const string SessionId = "session_id";
}