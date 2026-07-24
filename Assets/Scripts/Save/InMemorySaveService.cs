namespace SurviveUntilPayday.Save
{
    /// <summary>
    /// EditMode/단위 테스트용 메모리 저장소.
    /// </summary>
    public sealed class InMemorySaveService : ISaveService
    {
        private string contents;
        private bool exists;

        public bool Exists()
        {
            return exists;
        }

        public string ReadAllText()
        {
            return exists ? contents : null;
        }

        public void WriteAllText(string text)
        {
            contents = text ?? string.Empty;
            exists = true;
        }

        public void Delete()
        {
            contents = null;
            exists = false;
        }
    }
}
