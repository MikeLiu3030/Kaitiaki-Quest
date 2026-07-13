// Register Request
export interface RegisterRequest {
  email: string;
  password: string;
  userName: string;
}

// Login request
export interface LoginRequest {
  email: string;
  password: string;
}

// Authentication response
export interface AuthResponse {
  token: string;
  email: string;
  userName: string;
  totalXP: number;
  level: number;
  roles: string[];
}

// User information
export interface User {
  id: string;
  email: string;
  userName: string;
  totalXP: number;
  level: number;
  currentStreak: number;
  roles: string[];
}






