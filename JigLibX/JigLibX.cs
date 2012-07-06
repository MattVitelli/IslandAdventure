//---------------------------------------------------------------
// JigLibX BETA - XNA Physic Engine
// Copyright (C) Thorben Linneweber
// http://www.codeplex.com/JigLibX
//
// JiggleX is a port of the C++ Physic Engine "JigLib"
// Copyright (C) 2004 Danny Chapman 
// http://www.rowlhouse.co.uk/jiglib/index.html
//---------------------------------------------------------------
// Thanks also go to raxxla, DeanoC and cr125rider
// Visit the JigLibX Wiki http://jiglibx.wikidot.com/
//---------------------------------------------------------------
//
// Design Guidelines
// http://msdn2.microsoft.com/en-us/library/czefa0ke(vs.71).aspx
//
// BUGS:
// - Sometimes objects freeze in midAir. This is a bug of the orig JigLib Library. It will be fixed
//   when the beta phase is closed. (fixed)
// - Car bounces arround when killing a ragdoll. (fixed)
//
// TODO:
//
// Alpha
// - SolverAccumulated doesnt work (fixed)
// - SolverNormal explodes sometimes (fixed)
// - Hanging deactivated objects doesn't always reactivate (fixed)
// - Add the vehicle classes (done)
// - Static Mesh support missing (done)
// - CollisionSystem Grid behaves stranges with very large objects (fixed)
// - WorldInertia and WorldInvInertia got calculated wrong. (fixed)
// - RemoveCollisionSkin bug (fixed)
// - Check inertia tensor again (done)
// - Change the mVariable names to variable (in vehicle and constraint classes) (done)
//
// Beta
// - HighFrequency code REFERENCE's. (Especially operator overloads for Vector3 and Matrix structs) (mostly done)
//   Replace matrix0 = matrix1*matrix2 with Matrix.Multiply(ref matrix1,ref matrix2, out matrix0)
//   Design Guidelines:
//   + Public methods never have a ref parameter
//   + Use ref parameters for structs in internal/private classes only whent its high frequency code
//     (In the detection classes for example)
//   + Mark reference calls with #region REFERENCE or manual inline calls with #region INLINE
// - Distance,Overlap and Intersect classes have to be optimized
// - REFACTORIZE
