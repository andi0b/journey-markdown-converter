namespace journey_markdown_converter
 
open CommandLine
 
type MdConverter =
    { RmdConverter: ReverseMarkdown.Converter
      Options: Options }

module MdConverter =
    let convertHtml mdConverter html = mdConverter.RmdConverter.Convert html

    let createFrmSettings options =
        let config = ReverseMarkdown.Config()
        config.GithubFlavored <- options.MdGithubFlavoured
        config.ListBulletChar <- options.MdListBulletChar
        config.SmartHrefHandling <- options.MdSmartHrefHandling
        
        { RmdConverter = ReverseMarkdown.Converter(config)
          Options = options }
