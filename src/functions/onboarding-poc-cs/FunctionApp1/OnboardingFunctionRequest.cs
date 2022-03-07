namespace Dome9.CloudGuardOnboarding.Azure
{
    internal class OnboardingFunctionRequest
    {
        public string OnboardingId { get; set; }
        public string ApiKey { get; set; }    
        public string Secret { get; set; } 
        public string ApiBaseUrl { get; set; }
        public string Message { get; set; }           
    }
}
