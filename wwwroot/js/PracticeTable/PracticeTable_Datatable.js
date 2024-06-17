$(document).ready(function () {
    document.title = 'Practice Table';
  
    $("#tblPractice").DataTable({
        paging: true,
        select: true,
        "order": [[0, "desc"]],
        dom: 'Bfrtip',

        buttons: [
            'pageLength',
        ],

        "processing": true,
        "serverSide": true,
        "filter": true, //Search Box
        "orderMulti": false,
        "stateSave": true,

        "ajax": {
            "url": "/PracticeTable/GetDataTabelData",
            "type": "POST",
            "datatype": "json",
            "dataSrc": function (data) {
                console.log(data.data)
                // Assuming the server returns an object with a property named 'data'
                // console.log(data);
                // containing the array of objects to be displayed in the table
                return data.data;
            }
        },

        "columns": [
            {
                data: "Id", "name": "Id", render: function (data, type, row, meta) {
                    var sequence = meta.row + 1;
                    return "<a href='#' class='fa fa-eye' onclick=Details('" + row.Id + "');>" + sequence + "</a>";
                }
            },
            {
                data: null, "sortable": false, render: function (data, type, row) {
                    return "<a href='#' class='d-block' onclick=ViewImage('" + row.ProfilePath + "','Profile_Picture');><div class='image'><img src='" + row.ProfilePath + "' class='img50px img-circle elevation-2' alt='Asset Image'></div></a>";
                }
            },
            { "data": "Title", "name": "Title" },
            { "data": "Description", "name": "Description" },
            { "data": "ShortDescription", "name": "ShortDescription" },
            { "data": "Hospital", "name": "Hospital" },
            {
                data: null, render: function (data, type, row) {
                    
                    return "<a href='#' class='btn btn-info btn-xs' onclick=AddEdit('" + row.Id + "');>Edit</a>";
                }
            },
            {
                data: null, render: function (data, type, row) {
                    return "<a href='#' class='btn btn-danger btn-xs' onclick=Delete('" + row.Id + "'); >Delete</a>";
                }
            }
        ],

        'columnDefs': [{
            'targets': [6, 7],
            'orderable': false
        }],

        "lengthMenu": [[20, 10, 15, 25, 50, 100, 200], [20, 10, 15, 25, 50, 100, 200]],

        "initComplete": function (settings, json) {
            var column = this.api().column(5); // Index of the "Hospital" column (0-based index)
            var data = column.data().toArray(); // Convert to array
            var isEmpty = data.every(function (value) {
                return value === null || value === ''; // Check for null or empty strings
            });
            column.visible(!isEmpty);
        }

    });
});