
SELECT *
FROM results
ORDER BY finish_position ASC;


SELECT completions, count(1)
FROM results
GROUP BY completions
ORDER BY completions;

SELECT wave_name, count(1)
FROM results
GROUP BY wave_name
ORDER BY wave_name;


SELECT *
FROM results
where full_name = 'michael wildman';


with v as (
     --- my result
     select
        wave_id, finish_time
     from results
     where full_name = 'michael wildman'
),
d as (
     --- de-normalize the results of everyone so they are relative to the wall-clock time of my finish.
     --- e.g. if someone was 2 waves ahead, i would have to beat their finish_time by >10 minutes to have passed them.
    select
        e.*, e.finish_time - (  ((v.wave_id - e.wave_id) * 5) * interval '1 minutes') as non_normailzed_finish
    from entrants e, v
    where e.wave_id < v.wave_id
)
-- --- how many people did i pass?
-- select *
-- from d, v
-- where d.non_normailzed_finish > v.finish_time;

-- --- how many did i pass in each wave?
-- select d.wave_id, count(1)
-- from d, v
-- where d.non_normailzed_finish > v.finish_time
-- group by d.wave_id
-- order by d.wave_id;

--- who did i pass in a given wave?
select *
from d, v
where d.non_normailzed_finish > v.finish_time
and d.wave_id = 2
order by d.finish_time;
