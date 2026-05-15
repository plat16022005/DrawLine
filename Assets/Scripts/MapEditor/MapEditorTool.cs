/// <summary>
/// Các công cụ có trong Map Editor.
/// </summary>
public enum MapEditorTool
{
    None,
    TilePaint,      // Vẽ tile lên tilemap
    TileErase,      // Xóa tile khỏi tilemap
    PlacePlayer,    // Đặt vị trí spawn của người chơi
    PlaceDemon,     // Đặt vị trí spawn của quỷ
    PlacePrincess,  // Đặt vị trí spawn của công chúa + lồng
    PlaceTrap       // Đặt bẫy (sẽ mở panel nhập thông số)
}
