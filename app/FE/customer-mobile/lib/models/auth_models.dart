int _readInt(Map<String, dynamic> json, String camel, [String? pascal]) {
  final raw = json[camel] ?? (pascal != null ? json[pascal] : null);
  if (raw is int) return raw;
  if (raw is num) return raw.toInt();
  return int.tryParse(raw?.toString() ?? '') ?? 0;
}

String _readString(Map<String, dynamic> json, String camel, [String? pascal]) {
  final raw = json[camel] ?? (pascal != null ? json[pascal] : null);
  return raw?.toString() ?? '';
}

class UserInfo {
  final int id;
  final String fullName;
  final String email;
  final String role;
  final String? avatarUrl;
  final String? phoneNumber;
  final int? loyaltyPoints;
  final String? membershipLevel;

  UserInfo({
    required this.id,
    required this.fullName,
    required this.email,
    required this.role,
    this.avatarUrl,
    this.phoneNumber,
    this.loyaltyPoints,
    this.membershipLevel,
  });

  factory UserInfo.fromJson(Map<String, dynamic> json) {
    return UserInfo(
      id: _readInt(json, 'id', 'Id'),
      fullName: _readString(json, 'fullName', 'FullName'),
      email: _readString(json, 'email', 'Email'),
      role: _readString(json, 'role', 'Role'),
      avatarUrl: json['avatarUrl']?.toString() ?? json['AvatarUrl']?.toString(),
      phoneNumber: json['phoneNumber']?.toString() ?? json['PhoneNumber']?.toString(),
      loyaltyPoints: json['loyaltyPoints'] is int
          ? json['loyaltyPoints'] as int
          : (json['LoyaltyPoints'] is int ? json['LoyaltyPoints'] as int : null),
      membershipLevel: json['membershipLevel']?.toString() ?? json['MembershipLevel']?.toString(),
    );
  }
}

class TokenResponse {
  final String accessToken;
  final String refreshToken;
  final int expiresIn;
  final UserInfo user;
  final int sessionId;
  final int tableId;
  final int tableNumber;

  TokenResponse({
    required this.accessToken,
    required this.refreshToken,
    required this.expiresIn,
    required this.user,
    this.sessionId = 0,
    this.tableId = 0,
    this.tableNumber = 0,
  });

  factory TokenResponse.fromJson(Map<String, dynamic> json) {
    final userRaw = json['user'] ?? json['User'] ?? {};
    return TokenResponse(
      accessToken: _readString(json, 'accessToken', 'AccessToken'),
      refreshToken: _readString(json, 'refreshToken', 'RefreshToken'),
      expiresIn: _readInt(json, 'expiresIn', 'ExpiresIn'),
      user: UserInfo.fromJson(Map<String, dynamic>.from(userRaw as Map)),
      sessionId: _readInt(json, 'sessionId', 'SessionId'),
      tableId: _readInt(json, 'tableId', 'TableId'),
      tableNumber: _readInt(json, 'tableNumber', 'TableNumber'),
    );
  }
}

class GuestLoginResponse {
  final String token;
  final int sessionId;
  final int tableId;
  final int tableNumber;
  final String role;
  final String guestName;

  GuestLoginResponse({
    required this.token,
    required this.sessionId,
    required this.tableId,
    required this.tableNumber,
    required this.role,
    this.guestName = 'Guest',
  });

  factory GuestLoginResponse.fromJson(Map<String, dynamic> json) {
    final guestName = _readString(json, 'guestName', 'GuestName').trim();
    return GuestLoginResponse(
      token: _readString(json, 'token', 'Token'),
      sessionId: _readInt(json, 'sessionId', 'SessionId'),
      tableId: _readInt(json, 'tableId', 'TableId'),
      tableNumber: _readInt(json, 'tableNumber', 'TableNumber'),
      role: _readString(json, 'role', 'Role').isNotEmpty
          ? _readString(json, 'role', 'Role')
          : 'GUEST',
      guestName: guestName.isNotEmpty ? guestName : 'Guest',
    );
  }
}

/// Login/register CUSTOMER + gắn bàn trong 1 response.
class CustomerDiningLoginResponse {
  final String accessToken;
  final String refreshToken;
  final int expiresIn;
  final UserInfo user;
  final int sessionId;
  final int tableId;
  final int tableNumber;

  CustomerDiningLoginResponse({
    required this.accessToken,
    required this.refreshToken,
    required this.expiresIn,
    required this.user,
    required this.sessionId,
    required this.tableId,
    required this.tableNumber,
  });

  factory CustomerDiningLoginResponse.fromJson(Map<String, dynamic> json) {
    final userRaw = json['user'] ?? json['User'] ?? {};
    return CustomerDiningLoginResponse(
      accessToken: _readString(json, 'accessToken', 'AccessToken'),
      refreshToken: _readString(json, 'refreshToken', 'RefreshToken'),
      expiresIn: _readInt(json, 'expiresIn', 'ExpiresIn'),
      user: UserInfo.fromJson(Map<String, dynamic>.from(userRaw as Map)),
      sessionId: _readInt(json, 'sessionId', 'SessionId'),
      tableId: _readInt(json, 'tableId', 'TableId'),
      tableNumber: _readInt(json, 'tableNumber', 'TableNumber'),
    );
  }
}

/// Phiên ăn gắn với bàn — dùng chung cho GUEST và CUSTOMER sau khi quét QR.
class DiningContext {
  final int sessionId;
  final int tableId;
  final int tableNumber;

  const DiningContext({
    required this.sessionId,
    required this.tableId,
    required this.tableNumber,
  });

  bool get isValid => sessionId > 0 && tableId > 0 && tableNumber > 0;

  factory DiningContext.fromJson(Map<String, dynamic> json) {
    return DiningContext(
      sessionId: _readInt(json, 'sessionId', 'SessionId'),
      tableId: _readInt(json, 'tableId', 'TableId'),
      tableNumber: _readInt(json, 'tableNumber', 'TableNumber'),
    );
  }
}
