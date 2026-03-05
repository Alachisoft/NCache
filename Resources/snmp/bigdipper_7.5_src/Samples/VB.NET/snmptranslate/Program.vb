Imports Lextm.SharpSnmpLib.Mib
Imports Lextm.SharpSnmpLib

Module Program

    Sub Main(ByVal args As String())
        If args.Length <> 1 Then
            Console.WriteLine("This application takes one parameter.")
        End If

        Dim oid As String = args(0)
        Dim registry As IObjectRegistry = New ReloadableObjectRegistry("modules")
        Dim tree As IObjectTree = registry.Tree
        Dim o = tree.Search(ObjectIdentifier.Convert(oid))
        Dim textual As String = o.AlternativeText
        Console.WriteLine(textual)
        If o.GetRemaining().Count = 0 Then
            Console.WriteLine(o.Definition.Type.ToString())
        End If
    End Sub

End Module
