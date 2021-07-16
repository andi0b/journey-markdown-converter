namespace journey_markdown_converter

type MdConverter =
    { RmdConverter: ReverseMarkdown.Converter
      Options: CommandLineOptions }

module MdConverter =
    let convertHtml mdConverter html = mdConverter.RmdConverter.Convert html

    let createFromOptions options =
        let config = ReverseMarkdown.Config()
        config.GithubFlavored <- options.MdGithubFlavoured
        config.ListBulletChar <- options.MdListBulletChar
        config.SmartHrefHandling <- options.MdSmartHrefHandling
        config.PassThroughTags <- options.MdPassThroughTags
        config.UnknownTags <- options.MdUnknownTags
        config.TableWithoutHeaderRowHandling <- options.MdTableWithoutHeaderRowHandling
        config.WhitelistUriSchemes <- options.MdWhitelistUriSchemes

        { RmdConverter = ReverseMarkdown.Converter(config)
          Options = options }
