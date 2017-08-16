# Searchie
My final site gives the user the full end to end experience. It starts with the index.html page, where the user can perform searches. The searches search two separate things, the NBA players, and the stored URLs from the crawler. The results are then shown, and an img is taken from the site to show the user more information. Next, my site has a dashboard.html location, which allows the user to control the crawler and watch some of the primary information about the site. On the dashboard, there is a graph which shows the RAM and the CPU usage for the last minute. This display is helpful because you can watch for spikes in the CPU when you are working with the crawler. The crawler is crawling sites from two domains, cnn and bleacherreport. The crawler evaluates the date of the articles and if the date was within the last two months, it stores it into the index. My code is written in C# and I did by best to use C# best practices. The site is hosted on Azure (the index, dashboard, webrole, and worker) and AWS (the NBA search).

The search bar works 2 ways. One, while you are typing, it will automatically send the search queries (like Google), and two, if you hit enter on your desired search, it will search and remove the suggestions so you can easily see results.

### To Do
Still need to add screenshots to readme file

### Author
Ross Grambo