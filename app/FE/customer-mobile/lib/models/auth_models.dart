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
      id: json['id'] ?? 0,
      fullName: json['fullName'] ?? '',
      email: json['email'] ?? '',
      role: json['role'] ?? '',
      avatarUrl: json['avatarUrl'],
      phoneNumber: json['phoneNumber'],
      loyaltyPoints: json['loyaltyPoints'],
      membershipLevel: json['membershipLevel'],
    );
  }
}

class TokenResponse {
  final String accessToken;
  final String refreshToken;
  final int expiresIn;
  final UserInfo user;

  TokenResponse({
    required this.accessToken,
    required this.refreshToken,
    required this.expiresIn,
    required this.user,
  });

  factory TokenResponse.fromJson(Map<String, dynamic> json) {
    return TokenResponse(
      accessToken: json['accessToken'] ?? '',
      refreshToken: json['refreshToken'] ?? '',
      expiresIn: json['expiresIn'] ?? 0,
      user: UserInfo.fromJson(json['user'] ?? {}),
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
    return GuestLoginResponse(
      token: json['token'] ?? '',
      sessionId: json['sessionId'] ?? 0,
      tableId: json['tableId'] ?? 0,
      tableNumber: json['tableNumber'] ?? 0,
      role: json['role'] ?? 'GUEST',
      guestName: (json['guestName'] as String?)?.trim().isNotEmpty == true
          ? json['guestName'] as String
          : 'Guest',
    );
  }
}
