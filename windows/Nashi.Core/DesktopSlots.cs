namespace Nashi.Core;

/// Picks the free desktop grid cell closest to a source icon: right, below, left, above,
/// then the corners, widening ring by ring.
public static class DesktopSlots
{
    private const int Rings=8;
    public static (int X,int Y)? Next((int X,int Y) source,(int X,int Y) cell,IReadOnlyCollection<(int X,int Y)> occupied,Func<(int X,int Y),bool> allowed)
    {
        for(var ring=1;ring<=Rings;ring++)
            foreach(var (column,row) in new[]{(ring,0),(0,ring),(-ring,0),(0,-ring),(ring,ring),(-ring,ring),(ring,-ring),(-ring,-ring)})
            {
                var slot=(X:source.X+column*cell.X,Y:source.Y+row*cell.Y);
                if(allowed(slot)&&!occupied.Any(icon=>Math.Abs(icon.X-slot.X)<cell.X&&Math.Abs(icon.Y-slot.Y)<cell.Y))return slot;
            }
        return null;
    }
}
