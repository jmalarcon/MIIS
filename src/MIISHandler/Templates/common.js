//Manage the ToC links
//Make sure your ToC is included inside an element with the .miis-toc class applied
//Make sure your CSS has a .current-doc selector to highlight the currently selected document in the ToC

$(function(){
    //Make sure all external links open in a new tab
    $('a[href*="https://"], a[href*="http://"]').attr('target', '_blank');

    //Get the link pointing to the current document (without the slash at the beginning) and internal links only
    var navLinks = $('.miis-toc a').filter(function(){
        var link = $(this).attr('href');
        return !(link.toLowerCase().indexOf('http://') == 0 || link.toLowerCase().indexOf('https://') == 0);
    });
    var currPath = window.location.pathname;
    var currDoc = window.location.pathname.substr(currPath.lastIndexOf("/")+1);
    var currLink = navLinks.filter(function(){
        //Checks if the link points to the document, starting with "/", "./" or without the root slash.
        var hRefRE = new RegExp('^\.{0,1}\/{0,1}' + currDoc + '$', 'ig'); 
        var href = $(this).attr('href');
        if (href == '') return false;   //Empty links that normally are only placeholders to contain other sub-lists
        return hRefRE.test(href) || href==currPath;
    });
    currLink.addClass('current-doc');
    
    //Make side lists collapsible and open automatically the one pointing to the current document
    $('.miis-toc > ul > li > a').filter(function(){
            return ($(this).attr('href') != '');
        }).parent().find('ul > li').hide();
    
    currLink.parentsUntil('.miis-toc > ul').last().find('li').show()

    /*
    Get the Prev & Next links to set the navigation buttons
    For this to work the next button must have an id='next-button' and the previous button an id='prev-button'
    If you want the link to have the next/prev elements titles assigned, then it must have an element with class="title"
    */
    var posCurrDoc = navLinks.index(currLink);
    if (posCurrDoc >= 0) {
        if (posCurrDoc > 0) {
            $('#prev-button').attr('href', navLinks.eq(posCurrDoc-1).attr('href')).show();
            $('#prev-button .title').text(navLinks.eq(posCurrDoc-1).text());
        }
        else
            $('#prev-button').hide();

        if (posCurrDoc < navLinks.length-1) {
            $('#next-button').attr('href', navLinks.eq(posCurrDoc+1).attr('href')).show();
            $('#next-button .title').text(navLinks.eq(posCurrDoc+1).text());
        }
        else
            $('#next-button').hide();
    }
    else{
        $('#prev-button').hide();
        $('#next-button').hide();
    }
});



/* NOTE: 
--------------------------------------
 uncomment this code below if you don't want to have every menu item open in the "Material" theme.
 It expands only the current active tree and hides the other elements.
 the toc.md has following structure:
 
 ### My Menu
 
 #### [First Section](/firstsection/index)
* [Foo](/firstsection/foo/index)

#### [Second Section](/secondsection/index)
* [Aa](/secondsection/aa/index)
    * [Sub](/secondsection/aa/sub/index)
* [Bb](/secondsection/bb/index)
 
 The code checks for header elements (e.g. h4) and 'ul li' lists after each header.
 -------------------------------------
*/

////Manage the ToC links
////Make sure your ToC is included inside an element with the .miis-toc class applied
////Make sure your CSS has a .current-doc selector to highlight the currently selected document in the ToC
//// 2018-08-08: modified by W. Staelens

//// code block sets up the menu when the page first loads, and decides which UL or UL sublist to display based on current URL
//{
//	// Hide all UL's that are under .miis-toc
//	$('ul', '.miis-toc').hide();
	
//    // Look for a UL sublist after an anchor with href matching our browsers current pathname
//	var node = $('a[href="'+location.pathname+'"] + ul', '.miis-toc');
  
//	// If not found, look for a UL after an h4 with child anchors href matching our browsers current pathname
//    if (node.length === 0) {
//		//var a = $('h4 a[href="'+location.pathname+'"]', '.miis-toc');
//		// http://api.jquery.com/header-selector/
//		var a = $(':header a[href="'+location.pathname+'"]', '.miis-toc');
//		node = $('+ ul', a.parent());
//	}
  
//	// If we have found a UL or UL sublist, we need to walk back up the "menu tree" and "show" each node (ie UL and/or UL sublist)
//	if (node.length) {
//		var parent = node; 
//		// When we reach to top of the menu (ie .miis-toc), terminate the loop
//		while(!parent.hasClass('miis-toc')) {
//			parent.show();
//			parent = parent.parent();
//		}
//	}
//}

//$(function(){
//    //Make sure all external links open in a new tab
//    $('a[href*="https://"], a[href*="http://"]').attr('target', '_blank');

//    //Get the link pointing to the current document (without the slash at the beginning) and internal links only
//    var navLinks = $('.miis-toc a').filter(function(){
//        var link = $(this).attr('href');
//        return !(link.toLowerCase().indexOf('http://') == 0 || link.toLowerCase().indexOf('https://') == 0);
//    });
//    var currPath = window.location.pathname;
//    var currDoc = window.location.pathname.substr(currPath.lastIndexOf("/")+1);
//    var currLink = navLinks.filter(function(){
//        //Checks if the link points to the document, starting with "/", "./" or without the root slash.
//        var hRefRE = new RegExp('^\.{0,1}\/{0,1}' + currDoc + '$', 'ig'); 
//        var href = $(this).attr('href');
//        if (href == '') return false;   //Empty links that normally are only placeholders to contain other sub-lists
//        return hRefRE.test(href) || href==currPath;
//    });
//    currLink.addClass('current-doc');
    
//    //Make side lists collapsible and open automatically the one pointing to the current document
//    /*$('.miis-toc > ul > li > a').filter(function(){
//            return ($(this).attr('href') != '');
//     }).parent().find('ul > li').hide();*/
    
//    /*currLink.parentsUntil('.miis-toc > ul').last().find('li').show()*/

	
//	// check if we have an element with the .current-doc class under .miis-toc
//	var currentNode = $('.current-doc', '.miis-toc')
	
//	// when we didn't find an element with urrent-doc class... we are maybe on an article page, so try to search for this
//	if (currentNode.length == 0) {
//		// e.g. we have this: http://localhost/foo/bar/wow
//		// from this: /foo/bar/wow
//		// get this: /foo/bar/
//		// and see if that is an url that is us being used (startswith) in the navigation
		
//		var currPathParent = currPath.substr(0, currPath.lastIndexOf("/"));
		
//		var foundNavLink = navLinks.filter(function(){
//			var link = $(this).attr('href');
//			return link.toLowerCase().startsWith(currPathParent.toLowerCase());
//		})
		
//		if (typeof foundNavLink !== 'undefined' && foundNavLink.length > 0) {
//			// highlight the corresponding menu section in case we are on an article page
//			foundNavLink.addClass('current-doc')
			
//			// take the first found item (todo: maybe sort on length and take last one?)
//			var firstfound = foundNavLink[0];
			
//			var parent  = $('a[href="'+firstfound.pathname+'"]', '.miis-toc');
			
//			// make the menu tree visible starting from the found url
//			while (!parent.hasClass('miis-toc')) {
//				 parent.show()
//				 parent = parent.parent()
//			}
//		}
//	}
//	// we did find an element with .miis-toc
//	else if (currentNode.length) { 
//		var parent = currentNode
		
//		while(!parent.hasClass('miis-toc')) {
//			parent.show()
//			parent = parent.parent()
//		}
//	}
	
//    /*
//    Get the Prev & Next links to set the navigation buttons
//    For this to work the next button must have an id='next-button' and the previous button an id='prev-button'
//    If you want the link to have the next/prev elements titles assigned, then it must have an element with class="title"
//    */
//    var posCurrDoc = navLinks.index(currLink);
//    if (posCurrDoc >= 0) {
//        if (posCurrDoc > 0) {
//            $('#prev-button').attr('href', navLinks.eq(posCurrDoc-1).attr('href')).show();
//            $('#prev-button .title').text(navLinks.eq(posCurrDoc-1).text());
//        }
//        else
//            $('#prev-button').hide();

//        if (posCurrDoc < navLinks.length-1) {
//            $('#next-button').attr('href', navLinks.eq(posCurrDoc+1).attr('href')).show();
//            $('#next-button .title').text(navLinks.eq(posCurrDoc+1).text());
//        }
//        else
//            $('#next-button').hide();
//    }
//    else{
//        $('#prev-button').hide();
//        $('#next-button').hide();
//    }
//});
