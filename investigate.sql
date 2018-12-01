
SELECT *
FROM results
ORDER BY finish_position ASC;


SELECT completions, count(1)
FROM results
GROUP BY completions
ORDER BY completions;

