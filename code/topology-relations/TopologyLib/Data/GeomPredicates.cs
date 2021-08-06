using System;

namespace TopologyLib.Data
{
    [Flags]
    public enum GeomPredicates
    {
        None = 0,
        Disjoint = 1,                              //                     [FF* FF* ***]
        Intersects = 2,                            //                     [T** *** ***] || [*T* *** ***] || [*** T** ***] || [*** *T* ***]
        Covers = Intersects | 4,                   //                     [T** *** FF*] || [*T* *** FF*] || [*** T** FF*] || [*** *T* FF*]
        CoveredBy = Intersects | 8,                //                     [T*F **F ***] || [*TF **F ***] || [**F T*F ***] || [**F *TF ***]
        Touches = Intersects | 16,                 //                                      [FT* *** ***] || [F** T** ***] || [F** *T* ***]             
        Within = CoveredBy | 32,                   //                     [T*F **F ***]                                               
        Contains = Covers | 64,                    //                     [T** *** FF*]                                               
        Equals = Within | Contains | 128,          //                     [T*F **F FF*]                                               
        Crosses = Intersects | 256,                //                    ([0** *** ***] && (dim a == 1 || dim b == 1)))
        Overlaps = Intersects | 512                //                    ([T*T *** ***] &&  dim a < dim b)|| 
    }                                              //                    ([T** *** T**] && dim a > dim b) || 
}                                                  //                    
                                                   //  dim a == dim b && 
                                                   //                   (([T*T *** T**] && (dim == 0 || dim == 2)) ||  
                                                   //                    ([1*T *** T**] && dim == 1))