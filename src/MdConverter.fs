namespace journey_markdown_converter

type MdConverter =
    string -> string

module MdConverter =
    let createFromOptions options : MdConverter =
        let config = ReverseMarkdown.Config()
        config.GithubFlavored <- options.MdGithubFlavoured
        config.ListBulletChar <- options.MdListBulletChar
        config.SmartHrefHandling <- options.MdSmartHrefHandling
        config.PassThroughTags <- options.MdPassThroughTags
        config.UnknownTags <- options.MdUnknownTags
        config.TableWithoutHeaderRowHandling <- options.MdTableWithoutHeaderRowHandling
        config.WhitelistUriSchemes <- options.MdWhitelistUriSchemes

        ReverseMarkdown.Converter(config).Convert