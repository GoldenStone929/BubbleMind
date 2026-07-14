namespace GenericGachaRPG
{
    public interface ISaveService
    {
        bool HasSave { get; }
        PlayerState Load();
        void Save(PlayerState state);
        PlayerState Reset();
    }
}
