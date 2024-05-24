// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
$(function () {
    $('a[data-trigger="click"]').popover({
        container: 'body'
    });
    
    $('.popover-dismiss').popover({
        trigger: 'focus'
    })

    $('[data-toggle="tooltip"]').tooltip()
    
    $('#postOverviewFilter').on('input', function() {
        this.submit();
    });
    
    let privatePostBtn = $('#privatePostBtn');
    
    privatePostBtn.on('show.bs.popover', function() {
        let tooltipEl = $('[data-toggle="tooltip"]')
        tooltipEl.tooltip('hide');
        tooltipEl.tooltip('disable');
    });

    privatePostBtn.on('hide.bs.popover', function() {
        let tooltipEl = $('[data-toggle="tooltip"]')
        tooltipEl.tooltip('enable');
    });
})