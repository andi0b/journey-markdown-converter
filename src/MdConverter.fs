namespace journey_markdown_converter

open System.Text.RegularExpressions

type MdConverter = string -> string



module MdConverter =

    let tagPrefix prefix md =

        let tagRegex = Regex("#(\S+)")
        let replacer (match': Match) =
            $"#{prefix}{match'.Groups.[1].Value}"

        tagRegex.Replace(md, replacer)
        
    
    let createFromOptions options : MdConverter =
        let config = ReverseMarkdown.Config()
        config.GithubFlavored <- options.MdGithubFlavoured
        config.ListBulletChar <- options.MdListBulletChar
        config.SmartHrefHandling <- options.MdSmartHrefHandling
        config.PassThroughTags <- options.MdPassThroughTags
        config.UnknownTags <- options.MdUnknownTags
        config.TableWithoutHeaderRowHandling <- options.MdTableWithoutHeaderRowHandling
        config.WhitelistUriSchemes <- options.MdWhitelistUriSchemes

        let prefixer = tagPrefix options.TagPrefix
        
        ReverseMarkdown.Converter(config).Convert >> prefixer
