namespace TestBenner
{
    public class Response
    {
        public bool Success { get; set; } = true;

        public bool HasErrors { get; set; }

        public int Number {  get; set; }

        public List<string> Errors { get; set; } = new List<string>();

        public void AddError(string message)
        {
            this.Errors.Add(message);
            this.HasErrors = true;
        }
    }
}
