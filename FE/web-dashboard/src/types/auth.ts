export interface UserInfo {
  id: number;
  fullName: string;
  email: string;
  role: string;
}

export interface TokenResponse {
  accessToken: string;
  refreshToken: string;
  user: UserInfo;
}
