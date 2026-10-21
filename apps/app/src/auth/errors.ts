import type { AuthError } from "@supabase/auth-js";

// Supabase's messages are written for developers ("Invalid login credentials"); the common ones get our wording.
export function authErrorMessage(error: AuthError) {
  switch (error.code) {
    case "invalid_credentials":
      return "Wrong email or password.";
    case "over_email_send_rate_limit":
    case "over_request_rate_limit":
      return "Too many attempts. Wait a few minutes and try again.";
    case "same_password":
      return "That's your current password. Choose a new one.";
    default:
      return error.message;
  }
}
