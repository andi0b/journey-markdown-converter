namespace journey_markdown_converter
 
type MdConverter =
    { RmdConverter: ReverseMarkdown.Converter
      Options: CommandLine.Options }

module MdConverter =
    let convertHtml html mdConverter = mdConverter.RmdConverter.Convert html

    let createFrmSettings options =
        let config = ReverseMarkdown.Config()
        config.GithubFlavored <- true
        { RmdConverter = ReverseMarkdown.Converter(config)
          Options = options }
