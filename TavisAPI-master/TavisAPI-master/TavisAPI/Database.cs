using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Npgsql;
using Newtonsoft.Json.Linq;
using Microsoft.Extensions.Configuration;
using System.Globalization;
using TavisAPI.Models;

namespace TavisAPI
{
    public class Database
    {
        private readonly string _connectionString;

        public Database(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("Tavis");
        }

        public void InsertUser(string email, string uid)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();

            using var cmd = new NpgsqlCommand()
            {
                Connection = conn
            };

            cmd.CommandText = @$"
                                INSERT INTO
                                    users (firebase_id, email)
                                VALUES
                                    ('{uid}', '{email}')
                                ";

            cmd.ExecuteNonQuery();
            conn.Close();
        }

        public void UpdateUser(string email, string uid)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();

            using var cmd = new NpgsqlCommand()
            {
                Connection = conn
            };

            cmd.CommandText = @$"
                                UPDATE
	                                users
                                SET
	                                email = '{email}'
                                WHERE
	                                firebase_id = '{uid}'
                                ";

            cmd.ExecuteNonQuery();
            conn.Close();
        }

        public void InsertIngredient(string title, string unit, float amount, string userId)
        {
            CultureInfo customCulture = (CultureInfo)System.Threading.Thread.CurrentThread.CurrentCulture.Clone();
            customCulture.NumberFormat.NumberDecimalSeparator = ".";

            System.Threading.Thread.CurrentThread.CurrentCulture = customCulture;

            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();

            using var cmd = new NpgsqlCommand()
            {
                Connection = conn
            };

            cmd.CommandText = @$"
                                INSERT INTO
                                    user_owned_foods (user_id, food_supercategory_id, amount, amount_unit)
                                SELECT
                                    u.id, f.id, {amount}, '{unit}'
                                FROM
                                    users as u
                                INNER JOIN food_supercategories as f
                                    ON f.title = '{title}'
                                WHERE
                                    u.firebase_id = '{userId}'
                                ";

            cmd.ExecuteNonQuery();
            conn.Close();
        }

        public void InsertUserPreference(Store store)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();

            using var cmd = new NpgsqlCommand()
            {
                Connection = conn
            };

            cmd.CommandText = @$"
                                INSERT INTO
	                                user_store_preferences
                                SELECT
	                                users.id, {store.ChainId}
                                FROM
	                                users
                                WHERE
	                                users.firebase_id = '{store.UserId}'
                                ";
            cmd.ExecuteNonQuery();
            conn.Close();
        }

        public void DeleteIngredient(int id)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();

            using var cmd = new NpgsqlCommand()
            {
                Connection = conn
            };

            cmd.CommandText = @$"
                                DELETE FROM 
                                    user_owned_foods
                                WHERE id = {id}
                                ";

            cmd.ExecuteNonQuery();
            conn.Close();
        }

        public void DeleteUserPreference(Store store)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();

            using var cmd = new NpgsqlCommand()
            {
                Connection = conn
            };

            cmd.CommandText = @$"
                                DELETE FROM 
	                                user_store_preferences as pref
                                USING 
	                                users
                                WHERE
	                                users.firebase_id = '{store.UserId}' AND
	                                pref.store_chain_id = {store.ChainId};
                                ";

            cmd.ExecuteNonQuery();
            conn.Close();
        }

        public void DeleteUser(User user)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();

            using var cmd = new NpgsqlCommand()
            {
                Connection = conn
            };

            cmd.CommandText = $@"
                                DELETE FROM 
	                                user_store_preferences as pref
                                USING 
	                                users
                                WHERE
	                                users.id = pref.user_id AND
	                                users.firebase_id = '{user.UID}'
                                ";

            cmd.ExecuteNonQuery();

            cmd.CommandText = $@"
                                DELETE FROM 
	                                user_owned_foods as owned
                                USING 
	                                users
                                WHERE
	                                users.id = owned.user_id AND
	                                users.firebase_id = '{user.UID}'
                                ";

            cmd.ExecuteNonQuery();

            cmd.CommandText = @$"
                                DELETE FROM 
	                                users
                                WHERE
	                                users.firebase_id = '{user.UID}'
                                ";

            cmd.ExecuteNonQuery();
            conn.Close();
        }

        public JArray FetchRecipes(string search, string[] time, string ingredients, string type, string uid)
        {
            string result = "";

            string query = @$"
                                SELECT
                                    recipes.id, 
                                    recipes.title, 
                                    types.type, 
                                    recipes.time, 
                                    recipes.image_file_path, 
                                    COALESCE(COUNT(distinct product.food_supercategory_id)) + COALESCE(COUNT(distinct own.id), 0) as sort
                                FROM 
                                    recipes
                                INNER JOIN recipe_types as types
                                    ON types.id = recipes.type_id
                                INNER JOIN ingredients
                                    ON ingredients.recipe_id = recipes.id
                                INNER JOIN food_supercategories as food
                                    ON food.id = ingredients.category
                                LEFT JOIN product_category as product
                                    ON product.food_supercategory_id = food.id
                                LEFT JOIN discount_products as discount
                                    ON discount.id = product.discount_product_id AND
                                    (discount.valid_from <= CURRENT_DATE::date AND discount.valid_to >= CURRENT_DATE::date)
                                LEFT JOIN user_owned_foods as own
	                                ON own.user_id = (
		                                SELECT
			                                id
		                                FROM 
			                                users
		                                WHERE
			                                firebase_id = '{uid}'
		                                ) AND own.food_supercategory_id = food.id
                                WHERE
                                    lower(recipes.title) LIKE lower('%{search}%')
                             ";

            if (!string.IsNullOrEmpty(type))
            {
                var arr = type.Split('-');
                query += $" AND (";

                foreach (string a in arr)
                {
                    query += $"types.type = '{a}'";

                    if (a != arr[^1])
                    {
                        query += " OR ";
                    }
                }

                query += ")";
            }

            if (time.Length > 0)
            {
                query += " AND (";

                foreach (string t in time)
                {
                    var arr = t.Split('-');
                    query += $"(time >= {arr[0]} AND time <= {arr[1]})";

                    if (t != time[^1])
                    {
                        query += " OR ";
                    }
                }

                query += ")";
            }

            query += " GROUP BY recipes.id, recipes.title, types.type";

            if (!string.IsNullOrEmpty(ingredients))
            {
                var arr = ingredients.Split("-");
                query += $" HAVING";

                foreach(string a in arr)
                {
                    query += $" '{a}' = ANY(ARRAY_AGG(food.title))";

                    if (a != arr[^1])
                    {
                        query += " AND";
                    }
                }
            }

            query += " ORDER BY sort DESC";

            string jsonQuery = $@"SELECT json_agg(t) FROM ({query}) t";

            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();

            using (var cmd = new NpgsqlCommand(jsonQuery, conn))
            {
                using NpgsqlDataReader rdr = cmd.ExecuteReader();

                while (rdr.Read())
                {
                    result += rdr["json_agg"].ToString();
                }
            }

            conn.Close();


            if (string.IsNullOrEmpty(result))
                return null;

            return JArray.Parse(result);
        }

        public JArray FetchIngredients()
       {
            var result = "";
            var query = @"
                         SELECT json_agg(t) FROM (
                            select 
	                            cat.title as value, cat.title as label
                            FROM 
	                            ingredients ing
                            INNER JOIN food_supercategories as cat
	                            ON cat.id = ing.category
                            GROUP BY
	                            cat.title
                            HAVING
	                            COUNT(category) >= 8
                        ) t
                         ";

            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();

            using (var cmd = new NpgsqlCommand(query, conn))
            {
                using NpgsqlDataReader rdr = cmd.ExecuteReader();

                while (rdr.Read())
                {
                    result += rdr["json_agg"].ToString();
                }
            }

            conn.Close();

            return JArray.Parse(result);
       }

        public JArray FetchRefrigerator(string uid)
        {
            var result = "";
            var query = $@"
                            SELECT json_agg(t) FROM (
	                            select 
		                            cat.title, own.amount, own.amount_unit, own.id
	                            FROM 
		                            user_owned_foods own
	                            INNER JOIN food_supercategories as cat
		                            ON cat.id = own.food_supercategory_id
	                            INNER JOIN users as u
		                            ON u.id = own.user_id
	                            WHERE
		                            u.firebase_id = '{uid}'
                            ) t
                         ";

            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();

            using (var cmd = new NpgsqlCommand(query, conn))
            {
                using NpgsqlDataReader rdr = cmd.ExecuteReader();

                while (rdr.Read())
                {
                    result += rdr["json_agg"].ToString();
                }
            }

            conn.Close();

            if (string.IsNullOrEmpty(result))
                return null;

            return JArray.Parse(result);
        }

        /*
         * 					INNER JOIN user_store_preferences as pref
							ON pref.store_chain_id = store.id AND
							pref.user_id = (
								SELECT
									id
								FROM
									users
								WHERE
									firebase_id = '8q5ffun0Kna9wv8CeVftB2JPvMH3'
							)
        */

        public JArray FetchRecipe(int id, string uid)
        {
            var result = "";
            var userPref = "";

            if (!string.IsNullOrEmpty(uid))
            {
                userPref = $@"
          					INNER JOIN user_store_preferences as pref
							    ON pref.store_chain_id = store.id AND
							pref.user_id = (
								SELECT
									id
								FROM
									users
								WHERE
									firebase_id = '{uid}'
							)
                             ";
            }

            var query = $@"
                            SELECT json_agg(t) FROM (
                                SELECT 
                                    recipes.id, 
                                    recipes.title, 
                                    recipes.instructions, 
                                    recipes.time, 
                                    recipes.amount_unit, 
                                    recipe_types.type, 
                                    recipes.image_file_path,
                                    (
                                        SELECT json_agg(t) FROM (
                                            SELECT
                                                ingredients.id as id,
                                                food_supercategories.title as title,
                                                ingredients.amount as amount,
                                                ingredients.unit as unit,
                                                (
                                                    SELECT json_agg(t) FROM (
                                                        SELECT DISTINCT
								                            store.name as store,
								                            discount.title,
								                            discount.price,
								                            discount.amount,
								                            discount.unit,
                                                            discount.valid_from,
                                                            discount.valid_to,
                                                            product.match_ratio
							                            FROM
								                            discount_products as discount
							                            INNER JOIN product_category as product
								                            ON product.discount_product_id = discount.id
							                            INNER JOIN discount_catalogues as catalogue
								                            ON catalogue.id = discount.catalogue_id
							                            INNER JOIN store_chains as store
								                            ON store.id = catalogue.store_chain_id
                                                        {userPref}
							                            WHERE 
								                            ingredients.category = product.food_supercategory_id AND
                                                            (discount.valid_from <= CURRENT_DATE::date AND discount.valid_to >= CURRENT_DATE::date)
							                            ORDER BY
								                            product.match_ratio DESC
                                                    ) t
                                                ) as discounts,
                                                (
						                            SELECT json_agg(t) FROM (
							                            SELECT
								                            owned.amount,
								                            owned.amount_unit
							                            FROM 
								                            user_owned_foods as owned
							                            INNER JOIN users
								                            on users.id = owned.user_id
							                            WHERE
								                            users.firebase_id = '{uid}' AND
								                            owned.food_supercategory_id = food_supercategories.id
						                            ) t
					                            ) as user_owned
                                            FROM    
                                                ingredients
                                            INNER JOIN food_supercategories 
                                                ON ingredients.category = food_supercategories.id
                                            WHERE
                                                ingredients.recipe_id = recipes.id
                                        ) t
                                    ) as ingredients
                                FROM
                                    recipes
                                INNER JOIN recipe_types 
                                    ON recipes.type_id = recipe_types.id
                                WHERE 
                                    recipes.id = {id}
                            ) t
                         ";

            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();

            using (var cmd = new NpgsqlCommand(query, conn))
            {
                using NpgsqlDataReader rdr = cmd.ExecuteReader();

                while (rdr.Read())
                {
                    result += rdr["json_agg"].ToString();
                }
            }

            conn.Close();

            if (string.IsNullOrEmpty(result))
                return null;

            JArray JSONresult = JArray.Parse(result);
            foreach (JObject r in JSONresult)
                r["instructions"] = JArray.FromObject(((string)r["instructions"]).Split("|"));

            return JSONresult;
        }

        public JArray FetchStores(string uid)
        {
            var result = "";
            var query = $@"
                            SELECT json_agg(t) FROM (
                                SELECT
	                                chains.id, 
	                                chains.name, 
	                                pref.user_id::bool as prefers
                                FROM
	                                store_chains as chains
                                LEFT JOIN user_store_preferences as pref
	                                 ON pref.store_chain_id = chains.id AND
	                                 pref.user_id = (
		                                 SELECT 
		 	                                id 
		                                 FROM 
		 	                                users 
		                                 WHERE 
		 	                                firebase_id = '{uid}'
	                                 )
                           ) t
                         ";

            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();

            using (var cmd = new NpgsqlCommand(query, conn))
            {
                using NpgsqlDataReader rdr = cmd.ExecuteReader();

                while (rdr.Read())
                {
                    result += rdr["json_agg"].ToString();
                }
            }

            conn.Close();

            if (string.IsNullOrEmpty(result))
                return null;

            return JArray.Parse(result);
        }
    }
}
