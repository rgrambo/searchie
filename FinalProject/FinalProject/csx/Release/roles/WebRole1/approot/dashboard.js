// Ross Grambo
// INFO 344
// Web Crawler
// May 19, 2015

// Load the Visualization API and the piechart package.
google.load('visualization', '1.0', { 'packages': ['corechart'] });

// Some globals (For chart)
var a = Array(10);
var index = 0;

// On document ready start auto refresh
$(document).ready(function () {
    setInterval(GetStatus, 4000);
});

// Clears queue and tables and stops worker
$("#clear").click(function () {
    $.ajax({
        type: "GET",
        url: "WebService2.asmx/Clear",
        success: function () {
            GetStatus();
        },
        error: function (xhr, ajaxOptions, thrownError) {
            alert(xhr.status);
            alert(thrownError);
        }
    });

    GetStatus();
});

// Starts the worker and reads robot.txt files
$("#start").click(function () {
    $.ajax({
        type: "GET",
        url: "WebService2.asmx/Start",
        success: function () {
            GetStatus();
        },
        error: function (xhr, ajaxOptions, thrownError) {
            alert(xhr.status);
            alert(thrownError);
        }
    });
});

// Search by inputting a url
$("#search").click(function () {
    var text = $("#searchtext").val();

    $.ajax({
        type: "GET",
        url: "WebService2.asmx/ReadWithWord",
        data: { 'search': text },
        datatype: "json",
        success: function (msg) {
            $("#result").empty();
            if (msg != "")
            {
                $("#result").text(decodeURIComponent(msg['0']['Url']));
            }
            else 
            {
                $("#result").text("No Results");
            }
        },
        error: function (xhr, ajaxOptions, thrownError) {
            alert(xhr.status);
            alert(thrownError);
        }
    });
});

// Refreshes the page with the new information
function GetStatus() {
    $.ajax({
        type: "GET",
        url: "WebService2.asmx/GetStatus",
        dataType: "json",
        success: function (msg) {
            counters = msg['1'].split("~~");

            $('#workers').text(msg['0']);
            $('#counters').text(counters[0]);
            $('#counters2').text(counters[1]);
            $('#numofurls').text(msg['2']);
            $('#lasturls').html(msg['3']);
            $('#numofqueue').text(msg['4']);
            $('#numofindex').text(msg['5']);
            $('#errors').text(msg['6']);


            a[index] = [parseFloat(counters[0]), parseFloat(counters[1]), parseFloat(msg['2']), parseFloat(msg['4']) / 100, parseFloat(msg['5'])];
            index++;
            if (index > 9) {
                index = 0;
            }
        },
        error: function (xhr, ajaxOptions, thrownError) {
            alert(xhr.status);
            alert(thrownError);
        }
    });

    $.ajax({
        type: "GET",
        url: "WebService1.asmx/getTrieCount",
        dataType: "json",
        success: function (msg) {
            $('#triecount').text(msg['0']);
            $('#trieword').text(msg['1']);
        },
        error: function (xhr, ajaxOptions, thrownError) {
            alert(xhr.status);
            alert(thrownError);
        }
    });

    makeChart();
}

// Creates the line chart
function makeChart() {
    // Create the data table.
    var temp = index;
    var a2 = a;

    var data = new google.visualization.arrayToDataTable([
        ['Seconds', 'RAM left (in Gb)', 'CPU (1=100%)'],
        getData(50 + ""),
        getData(45 + ""),
        getData(40 + ""),
        getData(35 + ""),
        getData(30 + ""),
        getData(25 + ""),
        getData(20 + ""),
        getData(15 + ""),
        getData(10 + ""),
        getData(5 + "")
    ]);

    // Reads 1 instance of data
    function getData(seconds) {

        if (a2[temp] != null) {
            var b = [seconds, a2[temp][0] / 1024, a2[temp][1] / 100]
        }
        else {
            var b = [seconds, 0, 0]
        }

        temp++;
        if (temp > 9) {
            temp = 0;
        }

        return b;
    }

    // Set chart options
    var options = {
        'title': 'Status Over Time',
        'width': 600,
        'height': 400
    };

    // Instantiate and draw our chart, passing in some options.
    var chart = new google.visualization.LineChart(document.getElementById('chart'));
    chart.draw(data, options);
}
