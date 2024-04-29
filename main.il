
//  Microsoft (R) .NET Framework IL Disassembler.  Version 4.8.3928.0
//  Copyright (c) Microsoft Corporation.  All rights reserved.



// Metadata version: v4.0.30319
.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .ver 4:0:0:0
}
.assembly main
{
  .hash algorithm 0x00008004
  .ver 0:0:0:0
}
.module main
// MVID: {E5C3925F-085A-43DD-B9C7-4F14124643F8}
.imagebase 0x00400000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000001    //  ILONLY
// Image base: 0x04E50000


// =============== CLASS MEMBERS DECLARATION ===================

.class private auto ansi Program
       extends [mscorlib]System.Object
{
  .method public static void  Main() cil managed
  {
    .entrypoint
    // Code size       13 (0xd)
    .maxstack  1
    .locals init ([0] string letter)
    IL_0000:  ldstr      "Hello World"
    IL_0005:  stloc.0
    IL_0006:  ldloc.0
    IL_0007:  call       void [mscorlib]System.Console::WriteLine(string)
    IL_000c:  ret
  } // end of method Program::Main

  .method public specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  2
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method Program::.ctor

} // end of class Program


// =============================================================

// *********** DISASSEMBLY COMPLETE ***********************
