export interface AuthResponseDto {
    isAuthSuccessful: boolean;
    errorMessage: string;
    token: string;
    is2StepVerificationRequired: boolean;
    provider: string;
    userName: string;
    myRole: string;
}