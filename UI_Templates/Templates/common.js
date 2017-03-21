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
        return hRefRE.test(href) || href==currPath;
    });
    currLink.addClass('current-doc');
    
    //Make side lists collapsible and open automatically the one pointing to the current document
    $('.miis-toc > ul > li > a').parent().find('ul > li').hide();
    currLink.parentsUntil('.miis-toc > ul').last().find('li').show()

    /*
    Get the Prev & Next links to set the navigation buttons
    For this to work the next button must have an id='next-button' and the previous button an id='prev-button'
    If you want the link to have the next/prev elements titles assigned, then it must have an element with class="title"
    */
    var posCurrDoc = navLinks.index(currLink);
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
});