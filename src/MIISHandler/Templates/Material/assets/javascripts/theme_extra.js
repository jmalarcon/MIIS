$(window).load(function(){
    /*
    Zooms automatically (with zoomify) all the images that are smaller than the available width when loading the page
    */
    //padding  for both left and right side
    var contentpadding = parseInt($('.article .wrapper').css("padding-left"));
    //Available width for images
    var contentWidth = $('.article .wrapper').innerWidth() - (contentpadding*2);
    $('.article img').each(function() {
        var img = $(this);
        var widthDiff = contentWidth-img.width();
        if ( widthDiff<10 ){ //Include images that almost fit in the available width (10px gap or less)
            img.zoomify();
        }
    });

    /*Empty links in the content are used to go back in navigation history */
    $('.article a[href=""]').attr('onclick', 'history.back();');

    /*Start and stop videos with one click */
    $('.article video').click(function(){
        if (this.paused)
            this.play();
        else
            this.pause();
    });
})