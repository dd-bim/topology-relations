
using GeometryLib.Int32.D2;
using GeometryLib.Int32;

using System.Collections.Generic;
using System.Linq;

namespace TopologyLib.Int32.D2
{
    public class Face
    {
        public HalfEdge RefEdge { get; internal set; }

        internal Face(HalfEdge firstHalfEdge)
        {
            RefEdge = firstHalfEdge;
            firstHalfEdge.RefFace = this;
        }

        //        internal static Face CreateTriangle(in IIntegerPoint a, in IIntegerPoint b, in IIntegerPoint c)
        //        {
        //            var ab = HalfEdge.Create(a, b);
        //            var bc = HalfEdge.Create(ab, c, true);
        //            var ca = HalfEdge.Create(bc, true, ab, true);

        ////#if DEBUG
        ////            if (a.EdgeSign(b, c) <= 0)
        ////            {
        ////                Console.WriteLine("Fehler");
        ////            }
        ////#endif
        //            //ab.Orig.refEdge = ab;
        //            return bc.RefFace = ca.RefFace = new Face(ab);
        //        }

        //        internal static Face CreateTriangle(in HalfEdge ab, in IIntegerPoint c)
        //        {
        //            var bc = HalfEdge.Create(ab, c, false);
        //            var ca = HalfEdge.Create(bc, true, ab, false);

        //            //// DEBUG:
        //            //if (c.EdgeSign(ab.Orig.Point, ab.Dest.Point) <= 0)
        //            //{
        //            //    Console.WriteLine("Fehler");
        //            //}
        //            //ab.Orig.refEdge = ab;
        //            return bc.RefFace = ca.RefFace = new Face(ab);
        //        }

        //        internal static Face CreateTriangle(in HalfEdge ab)
        //        {
        //            var ca = HalfEdge.Create(ab.Next, false, ab, false);

        //            //// DEBUG:
        //            //if (ab.Prev.Orig.Point.EdgeSign(ab.Orig.Point, ab.Dest.Point) <= 0)
        //            //{
        //            //    Console.WriteLine("Fehler");
        //            //}
        //            //ab.Orig.refEdge = ab;
        //            return ca.RefFace = ab.Next.RefFace = new Face(ab);
        //        }
        internal static Face CreateTriangle(in IIntegerPoint a, in IIntegerPoint b, in IIntegerPoint c, out Vertex av, out Vertex bv, out Vertex cv)
        {
            var ab = HalfEdge.Create(a, b);
            var bc = HalfEdge.Create(ab, c, true);
            var ca = HalfEdge.Create(bc, true, ab, true);

            av = ab.Orig;
            bv = bc.Orig;
            cv = ca.Orig;

            av.RefEdge = ab;
            bv.RefEdge = bc;
            cv.RefEdge = ca;

            //#if DEBUG
            //            if (a.EdgeSign(b, c) <= 0)
            //            {
            //                Console.WriteLine("Fehler");
            //            }
            //#endif
            //ab.Orig.refEdge = ab;
            return bc.RefFace = ca.RefFace = new Face(ab);
        }

        internal static Face CreateTriangle(in HalfEdge ab, in IIntegerPoint c, out Vertex cv)
        {
            var bc = HalfEdge.Create(ab, c, false);
            var ca = HalfEdge.Create(bc, true, ab, false);

            ab.Orig.RefEdge = ab;
            bc.Orig.RefEdge = bc;

            cv = ca.Orig;
            cv.RefEdge = ca;

            //// DEBUG:
            //if (c.EdgeSign(ab.Orig.Point, ab.Dest.Point) <= 0)
            //{
            //    Console.WriteLine("Fehler");
            //}
            //ab.Orig.refEdge = ab;
            return bc.RefFace = ca.RefFace = new Face(ab);
        }

        internal static Face CreateTriangle(in HalfEdge ab)
        {
            var ca = HalfEdge.Create(ab.Next, false, ab, false);
            ab.Orig.RefEdge = ab;
            ab.Next.Orig.RefEdge = ab.Next;
            ab.Prev.Orig.RefEdge = ab.Prev;


            //// DEBUG:
            //if (ab.Prev.Orig.Point.EdgeSign(ab.Orig.Point, ab.Dest.Point) <= 0)
            //{
            //    Console.WriteLine("Fehler");
            //}
            //ab.Orig.refEdge = ab;
            return ca.RefFace = ab.Next.RefFace = new Face(ab);
        }

        public IEnumerable<HalfEdge> Boundary()
        {
            HalfEdge cur = RefEdge;
            do
            {
                yield return cur;
                cur = cur.Next;
            } while (cur != RefEdge);
        }

        public HashSet<Face> BoundaryFaces()
        {
            var faces = new HashSet<Face>();
            foreach (var he in Boundary())
            {
                if (he.Twin.RefFace != null)
                {
                    _ = faces.Add(he.Twin.RefFace);
                }
            }
            return faces;
        }

        public IEnumerable<Face> All()
        {
            var faces = new HashSet<Face>();
            var queue = new Queue<Face>();
            queue.Enqueue(this);
            while (queue.Count > 0)
            {
                var cur = queue.Dequeue();
                if (faces.Add(cur))
                {
                    yield return cur;
                    foreach (var neigh in cur.BoundaryFaces())
                    {
                        if (!faces.Contains(neigh))
                        {
                            queue.Enqueue(neigh);
                        }
                    }
                }
            }
        }

        public override string ToString()
        {
            var verts = new List<string>();
            foreach (var e in Boundary())
            {
                verts.Add(e.Orig.ToString());
            }
            return string.Join("--", verts);
        }

    }
}
