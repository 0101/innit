module Console

#if DEBUG
let info x = Browser.Dom.console.info x
let warn x = Browser.Dom.console.info x
#else
let info x = ()
let warn x = ()
#endif