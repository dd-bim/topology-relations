using GeometryLib.Int64;

using System.Collections.Generic;

using I = GeometryLib.Int64.D2;
using F = GeometryLib.Int64.Fraction.D2;

namespace TopologyLib.Int64.D2
{
    public class HalfEdge
    {
        private Vertex _orig;
        private HalfEdge _next;
        private HalfEdge _prev;
        private HalfEdge _twin;

        public Vertex Orig
        {
            get => _orig;
            internal set
            {
                value.RefEdge = this;
                _orig = value;
            }
        }

        public Face? RefFace { get; internal set; }

        public HalfEdge Next
        {
            get => _next; internal set
            {
                _next = value;
                value._prev = this;
            }
        }

        public HalfEdge Twin
        {
            get => _twin; internal set
            {
                _twin = value;
                value._twin = this;
            }
        }

        public HalfEdge Prev
        {
            get => _prev; internal set
            {
                _prev = value;
                value._next = this;
            }
        }

        public HalfEdge? Collinear { get; internal set; }

        public Vertex Dest => Twin.Orig;

        public HalfEdge Right => Twin.Next;

        public HalfEdge Left => Prev.Twin;

        internal bool Flipable => RefFace != null && Twin != this && Twin.RefFace != null && Next != this && Next.Next == Prev;

        public bool IsHullEdge => RefFace is null || Twin.RefFace is null;

        public I.Vector VectorOrig
        {
            get
            {
                var cur = Twin;
                while (!(cur.Dest.Point is I.Vector))
                { // muss immer funktionieren da alle Kanten aus Teilung
                    cur = cur.Collinear!;
                }
                return (I.Vector)cur.Dest.Point;
            }
        }

        public I.Vector VectorDest
        {
            get
            {
                var cur = this;
                while (!(cur.Dest.Point is I.Vector))
                { // muss immer funktionieren da alle Kanten aus Teilung
                    cur = cur.Collinear!;
                }
                return (I.Vector)cur.Dest.Point;
            }
        }


        internal HalfEdge(in Vertex orig)
        {
            _orig = orig;
            //orig.refEdge = this;
            RefFace = null;
            _next = this;
            _twin = this;
            _prev = this;
            Collinear = null;
        }

        /*
         * o==d
         */
        internal static HalfEdge Create(in IIntegerPoint orig, in IIntegerPoint dest)
        {
            var origV = new Vertex(orig);
            var destV = new Vertex(dest);
            origV.RefEdge.Twin = destV.RefEdge;
            destV.RefEdge.Twin = origV.RefEdge;
            return origV.RefEdge;
        }

        /*
         * o==d
         */
        internal static HalfEdge Create(in Vertex orig, in IIntegerPoint dest)
        {
            var he = new HalfEdge(orig);
            orig.RefEdge = he;
            var destV = new Vertex(dest);
            he.Twin = destV.RefEdge;
            destV.RefEdge.Twin = he;
            return he;
        }

        /*
         * full: =prev=o==d=
         * else: d
         *       ‖
         * =prev=o=prev.next=
         */
        internal static HalfEdge Create(in HalfEdge prev, in IIntegerPoint dest, bool connectFull)
        {
            var he = new HalfEdge(prev.Dest);
            prev.Dest.RefEdge = he;
            var destV = new Vertex(dest);
            he.Twin = destV.RefEdge;
            destV.RefEdge.Twin = he;

            if (connectFull)
            {
                prev.Twin.Prev = he.Twin;
                he.Twin.Next = prev.Twin;
            }
            else
            {
                he.Twin.Next = prev.Next;
                prev.Next.Prev = he.Twin;
            }
            prev.Next = he;
            he.Prev = prev;

            return he;
        }

        /*
         * connectFull: =next=d==o=prev=
         * 
         *        else: =prev.next=o=prev=
         *                         ‖   
         *              =next.prev=d=next=            
         * 
         *        else:            o=prev=
         *                         ‖   
         *              =next.prev=d=next=            
         * 
         *        else: =prev.next=o=prev=
         *                         ‖   
         *                         d=next=            
         */
        internal static HalfEdge Create(in HalfEdge prev, bool connectFullPrev, in HalfEdge next, bool connectFullNext)
        {
            var he = new HalfEdge(prev.Dest);
            prev.Dest.RefEdge = he;
            var twin = new HalfEdge(next.Orig);
            next.Orig.RefEdge = twin;
            he.Twin = twin;
            twin.Twin = he;

            if (connectFullPrev)
            {
                prev.Twin.Prev = twin;
                twin.Next = prev.Twin;
            }
            else
            {
                twin.Next = prev.Next;
                prev.Next.Prev = twin;
            }

            if (connectFullNext)
            {
                twin.Prev = next.Twin;
                next.Twin.Next = twin;
            }
            else
            {
                twin.Prev = next.Prev;
                next.Prev.Next = twin;
            }

            prev.Next = he;
            he.Prev = prev;
            next.Prev = he;
            he.Next = next;

            return he;
        }

        /*
         *  o=this=v=newNext=d      
         */
        //internal HalfEdge Split(in Vertex vertex, bool collinear)
        //{
        // TODO: refEdge TwinOrig
        //    var newNext = vertex.RefEdge;
        //    var newTwin = new HalfEdge(Twin.Orig);

        //    newNext.Twin = newTwin;
        //    newTwin.Twin = newNext;

        //    newNext.Next = Next;
        //    Next.Prev = newNext;
        //    newNext.Prev = this;
        //    Next = newNext;

        //    Twin.Prev.Next = newTwin;
        //    newTwin.Prev = Twin.Prev;
        //    newTwin.Next = Twin;
        //    Twin.Prev = newTwin;

        //    Twin.Orig.RefEdge = newTwin;
        //    Twin.Orig = vertex;

        //    newNext.RefFace = RefFace;
        //    newTwin.RefFace = Twin.RefFace;

        //    if (collinear)
        //    {
        //        Collinear = newNext;
        //        newNext.Twin.Collinear = Twin;
        //    }

        //    return newNext;
        //}

        internal void Split(in HalfEdge newNext, bool collinear)//, bool setTwinOrigRefEdge)
        {
            //var newTwin = new HalfEdge(Twin.Orig);
            if (Twin.Orig.RefEdge.Equals(Twin) && !Twin.Equals(Twin.Twin))
                Twin.Orig.RefEdge = Twin.Orig.RefEdge.Left;

            //newNext.Twin = newTwin;
            //newTwin.Twin = newNext;

            newNext.Next = Next;
            Next.Prev = newNext;
            newNext.Prev = this;
            Next = newNext;

            Twin.Prev.Next = newNext.Twin;
            newNext.Twin.Prev = Twin.Prev;
            newNext.Twin.Next = Twin;
            Twin.Prev = newNext.Twin;


            Twin.Orig = newNext.Orig;

            newNext.RefFace = RefFace;
            newNext.Twin.RefFace = Twin.RefFace;
            newNext.Orig.RefEdge = newNext;

            if (collinear)
            {
                if (Collinear != null)
                {
                    Collinear.Twin.Collinear = newNext.Twin;
                    newNext.Collinear = Collinear;
                }
                Collinear = newNext;
                newNext.Twin.Collinear = Twin;
            }
        }

        internal bool FlipEdge(ref HashSet<HalfEdge> toFlip)
        {
            if (!Flipable)
            {
                return false;
            }

            var (ab, bc, ca, ba) = (this, Next, Prev, Twin);
            var (ad, db) = (ba.Next, ba.Prev);
            var a = ab.Orig;
            var b = bc.Orig;
            var c = ca.Orig;
            var d = db.Orig;

            if (!(((I.Vector)d.Point).InCircle((I.Vector)a.Point, (I.Vector)b.Point, (I.Vector)c.Point)))
            {
                return false;
            }

            // Flip
            var dc = ab;
            dc.RefFace!.RefEdge = dc;
            dc.Orig = d;
            d.RefEdge = dc;
            dc.Next = ca;
            ca.Prev = dc;
            ca.Next = ad;
            ad.Prev = ca;
            ad.Next = dc;
            dc.Prev = ad;
            ad.RefFace = dc.RefFace;

            var cd = ba;
            cd.RefFace!.RefEdge = cd;
            cd.Orig = c;
            c.RefEdge = cd;
            cd.Next = db;
            db.Prev = cd;
            db.Next = bc;
            bc.Prev = db;
            bc.Next = cd;
            cd.Prev = bc;
            bc.RefFace = cd.RefFace;

            a.RefEdge = ad;
            b.RefEdge = bc;

            if (bc.Flipable && !toFlip.Contains(bc.Twin))
            {
                toFlip.Add(bc);
            }
            if (ca.Flipable && !toFlip.Contains(ca.Twin))
            {
                toFlip.Add(ca);
            }
            if (ad.Flipable && !toFlip.Contains(ad.Twin))
            {
                toFlip.Add(ad);
            }
            if (db.Flipable && !toFlip.Contains(db.Twin))
            {
                toFlip.Add(db);
            }
            return true;
        }

        internal bool RemoveCollinearOrig()
        {
            var xb = this;
            var ax = Prev;
            var bx = Twin;
            var xa = Prev.Twin;

            if (ax.Collinear != xb || bx.Collinear != xa || xa != bx.Next)
                return false;

            // remove xb
            if (xb.RefFace != null)
                xb.RefFace.RefEdge = ax;

            ax.Next = xb.Next;
            xb.Next.Prev = ax;
            ax.Collinear = xb.Collinear;

            // remove xa
            if (xa.RefFace != null)
                xa.RefFace.RefEdge = bx;

            bx.Next = xa.Next;
            xa.Next.Prev = bx;
            bx.Collinear = xa.Collinear;

            ax.Twin = bx;
            bx.Twin = ax;

            return true;
        }

        internal bool DisconnectOrig(bool deleteThisFace = true)
        {
            if (RefFace is null || Twin.RefFace is null)
                return false;

            Prev.Next = Twin.Next;
            Twin.Next.Prev = Prev;

            Orig.RefEdge = Twin.Next;

            if (deleteThisFace)
            {
                Twin.RefFace.RefEdge = Next;
                //Next.RefFace = Twin.RefFace;
            }

            if (Twin.Collinear != null)
                Twin.Collinear.Twin.Collinear = null;

            return true;
        }

        public override string ToString() => $"{Orig}--{Dest}";

        public bool CollinearEdgeTo(I.Vector dest, out HalfEdge collinear)
        {
            foreach (var he in StarCcw())
                if (dest.InDirection(he.VectorOrig, he.VectorDest))
                {
                    collinear = he;
                    return true;
                }
            collinear = this;
            return false;
        }

        public IEnumerable<HalfEdge> StarCcw()
        {
            var cur = this;
            do
            {
                yield return cur;
                cur = cur.Left;
            } while (cur != this);
        }

        public IEnumerable<HalfEdge> StarCcwWithOut()
        {
            var cur = Left;
            while (cur != this)
            {
                yield return cur;
                cur = cur.Left;
            }
        }

        public IEnumerable<HalfEdge> StarCw()
        {
            var cur = this;
            do
            {
                yield return cur;
                cur = cur.Right;
            } while (cur != this);
        }

        public IEnumerable<HalfEdge> All()
        {
            var e = RefFace is null ? Right : this;
            if (e.RefFace != null)
            {
                foreach (var face in e.RefFace.All())
                {
                    foreach (var he in face.Boundary())
                    {
                        yield return he;
                        if (he.IsHullEdge)
                            yield return he.Twin;
                    }
                }
            }
            else
            {
                var cur = e;
                do
                {
                    yield return cur;
                    cur = cur.Next;
                } while (cur != e);
            }
        }

        public IEnumerable<HalfEdge> AllEdges()
        {
            var e = RefFace is null ? Right : this;
            if (e.RefFace != null)
            {
                var faces = new HashSet<Face>();
                foreach (var face in e.RefFace.All())
                {
                    _ = faces.Add(face);
                    foreach (var he in face.Boundary())
                    {
                        if (he.Twin.RefFace is null || he.Twin.RefFace == face || !faces.Contains(he.Twin.RefFace))
                        {
                            yield return he;
                        }
                    }
                }
            }
            else
            {
                while (e.Prev != e.Twin)
                    e = e.Prev;
                do
                {
                    yield return e;
                    e = e.Next;
                } while (e.Prev != e.Twin);
            }
        }

        internal void MakeNext(HalfEdge other)
        {
            Next = other;
            other.Prev = this;
            Next.Orig.RefEdge = Next;
        }

        public bool SameEnds(in HalfEdge other) => Orig == other.Orig && Dest == other.Dest;
    }
}
