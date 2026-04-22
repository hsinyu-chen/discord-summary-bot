namespace SummaryAndCheck.Options
{
    internal class GeminiOptions
    {
        public string? ApiKey { get; set; }
        public string? Prompts { get; set; }

        public string? WebPrompts { get; set; }

        public string Model { get; set; } = "gemini-2.5-flash";
        public string ApiVersion { get; set; } = "v1beta";

        public decimal PricePerMillionTokens_Input_TextVisualVideo { get; set; } = 0.30m;
        public decimal PricePerMillionTokens_Input_Audio { get; set; } = 1.00m;
        public decimal PricePerMillionTokens_Output { get; set; } = 2.50m;

        public float Temperature { get; set; } = 0.2f;
        public int ThinkingBudget { get; set; } = -1;
    }
}
