namespace WebSharper.UI.Next.Formlets.Tests

open WebSharper
open WebSharper.UI.Next
open WebSharper.UI.Next.Html

[<JavaScript>]
module Client =
    open WebSharper.JavaScript
    open WebSharper.UI.Next.Client
    open WebSharper.UI.Next.Formlets

    type Country =
        | [<Constant "HU">] HU
        | [<Constant "FR">] FR

    let Pair flX flY =
        Formlet.Return (fun x y -> (x, y))
        <*> flX
        <*> flY

    let Main() =
        Console.Log("Running JavaScript Entry Point..")
        let lm =
            ListModel.Create (fst >> fst) [
                (Key.Fresh(), "b"), (true, HU)
                (Key.Fresh(), "d"), (false, FR)
            ]
        let manyModel =
            Formlet.ManyWithModel lm (fun () -> (Key.Fresh(), "b"), (true, HU)) (fun item ->
                Formlet.Return (fun b (c, d) -> b, c, d)
                <*> (Controls.InputVar (item.Lens (fst >> snd) (fun ((a, b), cd) b' -> (a, b'), cd))
                    |> Formlet.WithTextLabel "First field:")
                <*> (Formlet.Do {
                        let! x = Controls.CheckBoxVar (item.Lens (snd >> fst) (fun (ab, (c, d)) c' -> ab, (c', d)))
                        let! y =
                            (if x then Controls.SelectVar else Controls.RadioVar)
                                (item.Lens (snd >> snd) (fun (ab, (c, d)) d' -> ab, (c, d')))
                                [HU, "Hungary"; FR, "France"]
                            |> Formlet.Horizontal
                        return x, y
                    }
                    |> Formlet.Horizontal))
        let f1 =
            Formlet.Return (fun (a, b) (c, d) -> a, b, c, d)
            <*> (Formlet.Return (fun x y -> x, y)
                <*> Controls.Input "a"
                <*> Controls.Input "b"
                |> Formlet.Horizontal)
            <*> (Formlet.Return (fun x y -> x, y)
                <*> Controls.CheckBox true
                <*> Controls.Select HU [HU, "Hungary"; FR, "France"]
                |> Formlet.Horizontal)
//        Formlet.Many f1
        Formlet.Do {
            let! res =
                manyModel
                |> Formlet.WithSubmit "Submit"
            return! Formlet.OfDoc (fun () ->
                res
                |> Seq.map (fun (b, c, d) ->
                    p [text (sprintf "%s %b %A" b c d)]
                    :> Doc)
                |> Doc.Concat)
        }
        |> Formlet.WithFormContainer
        |> Formlet.RunWithLayout Layout.Table (fun x ->
            Console.Log x
            JS.Window?foo <- x)

module Server =
    open WebSharper.Sitelets
    open WebSharper.UI.Next.Server

    [<Literal>]
    let TemplatePath = __SOURCE_DIRECTORY__ + "/index.html"

    type Template = Templating.Template<TemplatePath>

    [<Website>]
    let Website =
        Application.SinglePage (fun ctx ->
            Content.Doc(
                Template.Doc(main = [client <@ Client.Main() @>])
            )
        )
