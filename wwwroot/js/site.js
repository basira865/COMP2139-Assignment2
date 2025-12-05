// ==================== PART 3.1: AJAX Live Search ====================
$(document).ready(function () {
    console.log('✅ site.js loaded');

    // AJAX Live Search
    $('#searchInput, #categoryFilter').on('keyup change', function () {
        var searchTerm = $('#searchInput').val();
        var categoryId = $('#categoryFilter').val();

        console.log('🔍 Searching:', searchTerm, 'Category:', categoryId);

        $.ajax({
            url: '/Events/Search',
            type: 'GET',
            data: {
                search: searchTerm,
                categoryFilter: categoryId
            },
            success: function (result) {
                console.log('✅ Search successful');
                $('#eventsContainer').html(result);
            },
            error: function (xhr, status, error) {
                console.error('❌ Search failed:', error);
                $('#eventsContainer').html('<div class="alert alert-danger">Error loading events. Please try again.</div>');
            }
        });
    });
});