<?php
// Ross Grambo
// Info 344
// Search PHP for NBA Player Search
// April 13, 2015

	// Filters out everything that is not a letter or space
	$getname = preg_replace("/[^ \w]+/", "", $_GET["name"]);

	// Get the given name ()
	$name = $getname;

	// Connect to the database
	$username = 'info344user';
	$password = 'info344userpass';

	try {
		$conn = new PDO('mysql:host=nba.cfhqsb5yk7em.us-west-2.rds.amazonaws.com:3306;dbname=NBA', $username, $password);
		$conn->setAttribute(PDO::ATTR_ERRMODE, PDO::ERRMODE_EXCEPTION);

		$stmt = $conn->prepare('SELECT * FROM NBA_Players WHERE name = :name');
		$stmt->bindParam(':name', $name, PDO::PARAM_INT);
	    $stmt->execute();

	    $result = $stmt->fetch();
	 	
	 	// If a result is found, return it json_encoded
	    if ( count($result) ) 
	    {
		    echo $_GET['callback']. '(' . json_encode($result) . ')';
		}

	} catch(PDOException $e) {
		echo $_GET['callback']. '(' . 'ERROR: ' . $e->getMessage() . ')';
	}
