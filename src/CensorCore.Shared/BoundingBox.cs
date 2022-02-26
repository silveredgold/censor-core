namespace CensorCore;

public class BoundingBox
{
    public BoundingBox(float x, float y, float endX, float endY)
    {
        this.X = x;
        this.Y = y;
        this.Width = (int)Math.Ceiling(endX-x);
        this.Height = (int)Math.Ceiling(endY-y);
    }

    public float X { get; set; }
    public float Y { get; set; }
    public int Height { get; set; }
    public int Width { get; set; }
}
