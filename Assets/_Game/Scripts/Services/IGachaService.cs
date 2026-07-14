namespace GenericGachaRPG
{
    public interface IGachaService
    {
        GachaResult DrawSingle();
        GachaResult DrawSingle(string bannerId);
        GachaResult DrawSingle(GachaBannerDefinition banner);
        bool CanDrawSingle(string bannerId, out string reason);
        bool CanDrawSingle(GachaBannerDefinition banner, out string reason);
    }
}
