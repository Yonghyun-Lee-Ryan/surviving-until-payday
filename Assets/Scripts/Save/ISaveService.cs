namespace SurviveUntilPayday.Save
{
    /// <summary>
    /// 저장소 I/O 추상화. 테스트에서는 메모리 구현을 사용한다.
    /// </summary>
    public interface ISaveService
    {
        bool Exists();

        string ReadAllText();

        void WriteAllText(string contents);

        void Delete();
    }
}
