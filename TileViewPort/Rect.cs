using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


public class Rect    // Hmmm...fulfills part of IGridIterable, shows the need for more interfaces for the spectrum of geometry functionality...
{
    // For any particular Rect, x and y are relative to some origin 
    // (meanwhile, width and height are relative to x and y).
    // 
    // This origin may be the "global origin" of a Virtual Canvas coordinate system,
    //     (note that multiple Virtual Canvases may conceptually have origins (0,0) 
    //     at the same point, such as when multiple such are considered to be 
    //     vertically stacked, but such situations are beyond the scope of the Rect class;
    //     one must otherwise assume that two canvases do not share a common origin.)
    // or may be some other non-global origin, such as within another Rect,
    // or within any of various IGridIterable types.
    // 
    // The importance of this relative basis is this:
    //     To meaningfully compare or otherwise use two Rects together
    //     requires that they share the same origin.
    // Correct results from intersects() and overlapRect() require this.
    // 
    public int x      { get; private set; }
    public int y      { get; private set; }
    public int width  { get; private set; }
    public int height { get; private set; }

    public Rect(int xx, int yy, int ww, int hh) {
        x = xx;
        y = yy;
        width  = ww;
        height = hh;
    } // Rect()

    public int max_x() { return x + width;  }
    public int max_y() { return y + height; }

    public int min_x() { return x; }
    public int min_y() { return y; }

    public int center_x() { return width  / 2; }
    public int center_y() { return height / 2; }

    public bool intersects(Rect rr) {
        if (rr == null)
            return false;
        if (rr.max_x() < this.x)
            return false;
        if (rr.x > this.max_x())
            return false;
        if (rr.max_y() < this.y)
            return false;
        if (rr.y > this.max_y())
            return false;
        return true;
    } // intersects(rr)

    public bool intersects(int xx, int yy, int ww, int hh) {
        Rect rr = new Rect(xx, yy, ww, hh);
        return this.intersects(rr);
    } // intersects(x,y,w,h)

    public static bool intersects(int A_xx, int A_yy, int A_ww, int A_hh,
                                  int B_xx, int B_yy, int B_ww, int B_hh) {
        Rect A = new Rect(A_xx, A_yy, A_ww, A_hh);
        Rect B = new Rect(B_xx, B_yy, B_ww, B_hh);

        return A.intersects(B);
    } // intersects(x,y,w,h, x,y,w,h)

    public Rect overlapRect(Rect rr) {
        if (!this.intersects(rr))
            return null;

        // Determine the x origin of the intersection:
        int new_xx;
        if (rr.x < x)
            new_xx = x;
        else
            new_xx = rr.x;

        // Determine the y origin of the intersection:
        int new_yy;
        if (rr.y < y)
            new_yy = y;
        else
            new_yy = rr.y;

        // Determine the maximum x of the intersection:
        int new_max_x;
        if (rr.max_x() < max_x())
            new_max_x = rr.max_x();
        else
            new_max_x = max_x();

        // Determine the maximum y of the intersection:
        int new_max_y;
        if (rr.max_y() < max_y())
            new_max_y = rr.max_y();
        else
            new_max_y = max_y();

        // Determine width and height:
        int new_ww = new_max_x - new_xx;
        int new_hh = new_max_y - new_yy;

        Rect new_rect = new Rect(new_xx, new_yy, new_ww, new_hh);
        return new_rect;
    } // overlapRect(rr)

    public static Rect overlapRect(Rect A, int xx, int yy, int ww, int hh) {
        // This method overload may be redundant...
        Rect B = new Rect(xx, yy, ww, hh);
        return A.overlapRect(B);
    } // overlapRect(rr, x,y,w,h)

    public static Rect overlapRect(int A_xx, int A_yy, int A_ww, int A_hh,
                                   int B_xx, int B_yy, int B_ww, int B_hh) {
        Rect A = new Rect(A_xx, A_yy, A_ww, A_hh);
        Rect B = new Rect(B_xx, B_yy, B_ww, B_hh);
        return A.overlapRect(B);
    } // overlapRect(x,y,w,h, x,y,w,h)

    // What other Rect methods would be useful and interesting?

} // class Rect
