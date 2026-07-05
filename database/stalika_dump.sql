--
-- PostgreSQL database dump
--

\restrict JRWygu3zr8cST5qokgJE3GIl0JOi5cjM1QsiVYxVJ2PIBU34jzSvIugIbjHhHrk

-- Dumped from database version 18.3
-- Dumped by pg_dump version 18.3

-- Started on 2026-03-29 22:09:28

SET statement_timeout = 0;
SET lock_timeout = 0;
SET idle_in_transaction_session_timeout = 0;
SET transaction_timeout = 0;
SET client_encoding = 'UTF8';
SET standard_conforming_strings = on;
SELECT pg_catalog.set_config('search_path', '', false);
SET check_function_bodies = false;
SET xmloption = content;
SET client_min_messages = warning;
SET row_security = off;

--
-- TOC entry 2 (class 3079 OID 16389)
-- Name: pgcrypto; Type: EXTENSION; Schema: -; Owner: -
--

CREATE EXTENSION IF NOT EXISTS pgcrypto WITH SCHEMA public;


--
-- TOC entry 5096 (class 0 OID 0)
-- Dependencies: 2
-- Name: EXTENSION pgcrypto; Type: COMMENT; Schema: -; Owner: 
--

COMMENT ON EXTENSION pgcrypto IS 'cryptographic functions';


--
-- TOC entry 277 (class 1255 OID 16427)
-- Name: auto_book_for_tournament(date, integer); Type: FUNCTION; Schema: public; Owner: postgres
--

CREATE FUNCTION public.auto_book_for_tournament(p_date date, p_user_id integer) RETURNS void
    LANGUAGE plpgsql
    AS $$
DECLARE
    t_start TIME := '10:00';
    t_end TIME := '22:00';
    t TIME;
    table_id INT;
BEGIN
    FOR table_id IN SELECT table_number FROM tables WHERE gym_number = 2 LOOP
        t := t_start;
        WHILE t < t_end LOOP
            INSERT INTO bookings (user_id, table_number, start_time, end_time, total_price, booking_date)
            VALUES (p_user_id, table_id, p_date + t, p_date + t + INTERVAL '1 hour', 0, p_date);
            t := t + INTERVAL '1 hour';
        END LOOP;
    END LOOP;
END;
$$;


ALTER FUNCTION public.auto_book_for_tournament(p_date date, p_user_id integer) OWNER TO postgres;

--
-- TOC entry 278 (class 1255 OID 16428)
-- Name: check_inventory_quantity(); Type: FUNCTION; Schema: public; Owner: postgres
--

CREATE FUNCTION public.check_inventory_quantity() RETURNS trigger
    LANGUAGE plpgsql
    AS $$
BEGIN
    -- Проверка, есть ли достаточно инвентаря для аренды
    IF NEW.quantity > (SELECT quantity FROM equipment WHERE equipment_name = NEW.equipment_name) THEN
        RAISE EXCEPTION 'Недостаточно % в наличии. Доступно: %, требуется: %.',
            NEW.equipment_name, (SELECT quantity FROM equipment WHERE equipment_name = NEW.equipment_name), NEW.quantity;
    END IF;

    -- Если достаточно, уменьшаем количество
    UPDATE equipment
    SET quantity = quantity - NEW.quantity
    WHERE equipment_name = NEW.equipment_name;

    RETURN NEW;
END;
$$;


ALTER FUNCTION public.check_inventory_quantity() OWNER TO postgres;

--
-- TOC entry 279 (class 1255 OID 16429)
-- Name: check_slot_availability(); Type: FUNCTION; Schema: public; Owner: postgres
--

CREATE FUNCTION public.check_slot_availability() RETURNS trigger
    LANGUAGE plpgsql
    AS $$
BEGIN
    -- Проверка, существует ли уже бронирование для данного стола на указанное время
    IF EXISTS (
        SELECT 1
        FROM bookings
        WHERE table_number = NEW.table_number
          AND start_time = NEW.start_time
          AND booking_date = NEW.booking_date
    ) THEN
        RAISE EXCEPTION 'Этот слот уже забронирован для выбранного стола.';
    END IF;

    -- Если бронирования нет, разрешаем выполнение действия
    RETURN NEW;
END;
$$;


ALTER FUNCTION public.check_slot_availability() OWNER TO postgres;

--
-- TOC entry 280 (class 1255 OID 16430)
-- Name: check_table_booking(); Type: FUNCTION; Schema: public; Owner: postgres
--

CREATE FUNCTION public.check_table_booking() RETURNS trigger
    LANGUAGE plpgsql
    AS $$
BEGIN
    -- Проверка, если стол уже занят в это время
    IF EXISTS (
        SELECT 1
        FROM bookings
        WHERE table_number = NEW.table_number
        AND start_time = NEW.start_time
        AND booking_date = NEW.booking_date
    ) THEN
        RAISE EXCEPTION 'Этот слот уже забронирован для выбранного стола.';
    END IF;

    RETURN NEW;
END;
$$;


ALTER FUNCTION public.check_table_booking() OWNER TO postgres;

--
-- TOC entry 281 (class 1255 OID 16431)
-- Name: check_tournament_participants(); Type: FUNCTION; Schema: public; Owner: postgres
--

CREATE FUNCTION public.check_tournament_participants() RETURNS trigger
    LANGUAGE plpgsql
    AS $$
BEGIN
    -- Проверка, если количество участников в турнире не превышает максимальное
    IF (SELECT COUNT(*) FROM tournament_participants WHERE tournament_id = NEW.tournament_id) >= 
       (SELECT max_participants FROM tournaments WHERE tournament_id = NEW.tournament_id) THEN
        RAISE EXCEPTION 'Количество участников в турнире достигло максимального лимита.';
    END IF;

    RETURN NEW;
END;
$$;


ALTER FUNCTION public.check_tournament_participants() OWNER TO postgres;

--
-- TOC entry 282 (class 1255 OID 16432)
-- Name: decrease_equipment_quantity(); Type: FUNCTION; Schema: public; Owner: postgres
--

CREATE FUNCTION public.decrease_equipment_quantity() RETURNS trigger
    LANGUAGE plpgsql
    AS $$
DECLARE
    current_quantity INT;
BEGIN
    SELECT quantity INTO current_quantity
    FROM equipment
    WHERE equipment_name = NEW.equipment_name;

    IF NEW.quantity > current_quantity THEN
        RAISE EXCEPTION 'Недостаточно % в наличии. Доступно: %, требуется: %.',
            NEW.equipment_name, current_quantity, NEW.quantity;
    END IF;

    UPDATE equipment
    SET quantity = quantity - NEW.quantity
    WHERE equipment_name = NEW.equipment_name;

    RETURN NEW;
END;
$$;


ALTER FUNCTION public.decrease_equipment_quantity() OWNER TO postgres;

--
-- TOC entry 283 (class 1255 OID 16433)
-- Name: delete_bookings_for_tournament(date); Type: FUNCTION; Schema: public; Owner: postgres
--

CREATE FUNCTION public.delete_bookings_for_tournament(target_date date) RETURNS void
    LANGUAGE plpgsql
    AS $$
BEGIN
    DELETE FROM bookings
    WHERE booking_date = target_date
      AND table_number IN (
          SELECT table_number FROM tables WHERE gym_number = 2
      );
END;
$$;


ALTER FUNCTION public.delete_bookings_for_tournament(target_date date) OWNER TO postgres;

--
-- TOC entry 284 (class 1255 OID 16434)
-- Name: fn_active_bookings_count_by_table(); Type: FUNCTION; Schema: public; Owner: postgres
--

CREATE FUNCTION public.fn_active_bookings_count_by_table() RETURNS TABLE(table_number integer, booking_count integer)
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN QUERY
    SELECT table_number, COUNT(*)
    FROM bookings
    WHERE booking_date >= CURRENT_DATE
    GROUP BY table_number
    ORDER BY booking_count DESC;
END;
$$;


ALTER FUNCTION public.fn_active_bookings_count_by_table() OWNER TO postgres;

--
-- TOC entry 285 (class 1255 OID 16435)
-- Name: fn_check_equipment_availability(character varying, integer); Type: FUNCTION; Schema: public; Owner: postgres
--

CREATE FUNCTION public.fn_check_equipment_availability(in_name character varying, in_qty integer) RETURNS text
    LANGUAGE plpgsql
    AS $$
DECLARE
    available_qty INTEGER;
BEGIN
    SELECT quantity INTO available_qty
    FROM equipment
    WHERE equipment_name = in_name;

    IF available_qty IS NULL THEN
        RETURN 'Инвентарь не найден';
    ELSIF in_qty > available_qty THEN
        RETURN FORMAT('Недостаточно: доступно %s', available_qty);
    ELSE
        RETURN 'Доступно';
    END IF;
END;
$$;


ALTER FUNCTION public.fn_check_equipment_availability(in_name character varying, in_qty integer) OWNER TO postgres;

--
-- TOC entry 286 (class 1255 OID 16436)
-- Name: fn_check_slot_availability(integer, date, time without time zone, time without time zone); Type: FUNCTION; Schema: public; Owner: postgres
--

CREATE FUNCTION public.fn_check_slot_availability(in_table_number integer, in_date date, in_start time without time zone, in_end time without time zone) RETURNS text
    LANGUAGE plpgsql
    AS $$
DECLARE
    conflict_count INTEGER;
BEGIN
    SELECT COUNT(*) INTO conflict_count
    FROM bookings
    WHERE table_number = in_table_number
      AND booking_date = in_date
      AND start_time < in_end
      AND end_time > in_start;

    IF conflict_count > 0 THEN
        RETURN 'Стол занят';
    ELSE
        RETURN 'Стол свободен';
    END IF;
END;
$$;


ALTER FUNCTION public.fn_check_slot_availability(in_table_number integer, in_date date, in_start time without time zone, in_end time without time zone) OWNER TO postgres;

--
-- TOC entry 287 (class 1255 OID 16437)
-- Name: fn_user_booking_summary(integer); Type: FUNCTION; Schema: public; Owner: postgres
--

CREATE FUNCTION public.fn_user_booking_summary(in_user_id integer) RETURNS TABLE(total_bookings integer, total_paid numeric)
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN QUERY
    SELECT 
        COUNT(*),
        COALESCE(SUM(total_price), 0)
    FROM bookings
    WHERE user_id = in_user_id;
END;
$$;


ALTER FUNCTION public.fn_user_booking_summary(in_user_id integer) OWNER TO postgres;

--
-- TOC entry 292 (class 1255 OID 16438)
-- Name: fn_user_last_booking(integer); Type: FUNCTION; Schema: public; Owner: postgres
--

CREATE FUNCTION public.fn_user_last_booking(in_user_id integer) RETURNS TABLE(booking_date date, start_time time without time zone, table_number integer)
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN QUERY
    SELECT booking_date, start_time, table_number
    FROM bookings
    WHERE user_id = in_user_id
    ORDER BY booking_date DESC, start_time DESC
    LIMIT 1;
END;
$$;


ALTER FUNCTION public.fn_user_last_booking(in_user_id integer) OWNER TO postgres;

--
-- TOC entry 293 (class 1255 OID 16439)
-- Name: fn_user_total_spent(integer); Type: FUNCTION; Schema: public; Owner: postgres
--

CREATE FUNCTION public.fn_user_total_spent(in_user_id integer) RETURNS numeric
    LANGUAGE plpgsql
    AS $$
DECLARE
    total NUMERIC(10,2);
BEGIN
    SELECT COALESCE(SUM(total_price), 0)
    INTO total
    FROM bookings
    WHERE user_id = in_user_id;

    RETURN total;
END;
$$;


ALTER FUNCTION public.fn_user_total_spent(in_user_id integer) OWNER TO postgres;

--
-- TOC entry 300 (class 1255 OID 16440)
-- Name: increase_equipment_quantity(); Type: FUNCTION; Schema: public; Owner: postgres
--

CREATE FUNCTION public.increase_equipment_quantity() RETURNS trigger
    LANGUAGE plpgsql
    AS $$
BEGIN
    UPDATE equipment
    SET quantity = quantity + OLD.quantity
    WHERE equipment_name = OLD.equipment_name;

    RETURN OLD;
END;
$$;


ALTER FUNCTION public.increase_equipment_quantity() OWNER TO postgres;

--
-- TOC entry 301 (class 1255 OID 16441)
-- Name: update_inventory_on_cancellation(); Type: FUNCTION; Schema: public; Owner: postgres
--

CREATE FUNCTION public.update_inventory_on_cancellation() RETURNS trigger
    LANGUAGE plpgsql
    AS $$
BEGIN
    -- Возвращаем инвентарь в базу данных при отмене бронирования
    UPDATE equipment
    SET quantity = quantity + OLD.quantity
    WHERE equipment_name = OLD.equipment_name;

    RETURN OLD;
END;
$$;


ALTER FUNCTION public.update_inventory_on_cancellation() OWNER TO postgres;

--
-- TOC entry 303 (class 1255 OID 16442)
-- Name: xplowinventoryalert(); Type: PROCEDURE; Schema: public; Owner: postgres
--

CREATE PROCEDURE public.xplowinventoryalert()
    LANGUAGE plpgsql
    AS $$
DECLARE
    eq RECORD;
BEGIN
    FOR eq IN
        SELECT equipment_name, quantity
        FROM equipment
        WHERE quantity <= 3
    LOOP
        RAISE NOTICE 'Мало инвентаря: % (осталось: %)', eq.equipment_name, eq.quantity;
    END LOOP;
END;
$$;


ALTER PROCEDURE public.xplowinventoryalert() OWNER TO postgres;

--
-- TOC entry 304 (class 1255 OID 16443)
-- Name: xptopclients(integer); Type: PROCEDURE; Schema: public; Owner: postgres
--

CREATE PROCEDURE public.xptopclients(IN in_limit integer)
    LANGUAGE plpgsql
    AS $$
DECLARE
    rec RECORD;
BEGIN
    FOR rec IN
        SELECT u.first_name, u.last_name, COUNT(*) AS total
        FROM users u
        JOIN bookings b ON u.user_id = b.user_id
        GROUP BY u.user_id
        ORDER BY total DESC
        LIMIT in_limit
    LOOP
        RAISE NOTICE 'Клиент: % %, бронирований: %', rec.first_name, rec.last_name, rec.total;
    END LOOP;
END;
$$;


ALTER PROCEDURE public.xptopclients(IN in_limit integer) OWNER TO postgres;

--
-- TOC entry 305 (class 1255 OID 16444)
-- Name: xptrainerbookingstats(integer); Type: PROCEDURE; Schema: public; Owner: postgres
--

CREATE PROCEDURE public.xptrainerbookingstats(IN in_trainer_id integer)
    LANGUAGE plpgsql
    AS $$
DECLARE
    total_trainings INTEGER;
    total_income NUMERIC(10,2);
BEGIN
    SELECT COUNT(*), COALESCE(SUM(total_price), 0)
    INTO total_trainings, total_income
    FROM bookings
    WHERE coach_id = in_trainer_id;

    RAISE NOTICE 'Всего тренировок: %', total_trainings;
    RAISE NOTICE 'Общая сумма: % ₽', total_income;
END;
$$;


ALTER PROCEDURE public.xptrainerbookingstats(IN in_trainer_id integer) OWNER TO postgres;

SET default_tablespace = '';

SET default_table_access_method = heap;

--
-- TOC entry 220 (class 1259 OID 16445)
-- Name: bookings; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.bookings (
    booking_number integer NOT NULL,
    user_id integer NOT NULL,
    table_number integer NOT NULL,
    start_time timestamp without time zone NOT NULL,
    total_price numeric(10,2) NOT NULL,
    booking_date timestamp without time zone DEFAULT CURRENT_TIMESTAMP,
    coach_id integer,
    end_time timestamp without time zone NOT NULL,
    CONSTRAINT bookings_total_price_check CHECK ((total_price >= (0)::numeric))
);


ALTER TABLE public.bookings OWNER TO postgres;

--
-- TOC entry 221 (class 1259 OID 16456)
-- Name: equipment_rental; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.equipment_rental (
    rental_number integer NOT NULL,
    equipment_name character varying(100) NOT NULL,
    booking_number integer NOT NULL,
    quantity integer NOT NULL,
    amount numeric(10,2) NOT NULL,
    CONSTRAINT equipment_rental_amount_check CHECK ((amount >= (0)::numeric)),
    CONSTRAINT equipment_rental_quantity_check CHECK ((quantity > 0))
);


ALTER TABLE public.equipment_rental OWNER TO postgres;

--
-- TOC entry 222 (class 1259 OID 16466)
-- Name: trainers; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.trainers (
    trainer_id integer NOT NULL,
    user_id integer NOT NULL,
    hourly_rate numeric(10,2),
    qualification character varying(50),
    CONSTRAINT trainers_hourly_rate_check CHECK ((hourly_rate >= (0)::numeric))
);


ALTER TABLE public.trainers OWNER TO postgres;

--
-- TOC entry 223 (class 1259 OID 16472)
-- Name: users; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.users (
    user_id integer NOT NULL,
    first_name character varying(50) NOT NULL,
    email character varying(100) NOT NULL,
    password_hash text NOT NULL,
    role_id integer NOT NULL,
    created_at timestamp without time zone DEFAULT CURRENT_TIMESTAMP,
    updated_at timestamp without time zone DEFAULT CURRENT_TIMESTAMP,
    phone character varying(20) NOT NULL,
    last_name character varying(50)
);


ALTER TABLE public.users OWNER TO postgres;

--
-- TOC entry 224 (class 1259 OID 16485)
-- Name: booking_info; Type: VIEW; Schema: public; Owner: postgres
--

CREATE VIEW public.booking_info AS
 SELECT b.booking_number,
    b.booking_date,
    b.start_time,
    b.end_time,
    b.table_number,
    (((u.first_name)::text || ' '::text) || (u.last_name)::text) AS client_name,
    COALESCE((((tu.first_name)::text || ' '::text) || (tu.last_name)::text), ''::text) AS trainer_name,
    b.total_price,
    er.equipment_name,
    er.quantity AS equipment_quantity,
    er.amount AS equipment_amount
   FROM (((public.bookings b
     JOIN public.users u ON ((u.user_id = b.user_id)))
     LEFT JOIN public.users tu ON ((tu.user_id = ( SELECT trainers.user_id
           FROM public.trainers
          WHERE (trainers.trainer_id = b.coach_id)))))
     LEFT JOIN public.equipment_rental er ON ((er.booking_number = b.booking_number)))
  ORDER BY b.booking_date DESC, b.start_time;


ALTER VIEW public.booking_info OWNER TO postgres;

--
-- TOC entry 225 (class 1259 OID 16490)
-- Name: bookings_booking_number_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public.bookings_booking_number_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.bookings_booking_number_seq OWNER TO postgres;

--
-- TOC entry 5097 (class 0 OID 0)
-- Dependencies: 225
-- Name: bookings_booking_number_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public.bookings_booking_number_seq OWNED BY public.bookings.booking_number;


--
-- TOC entry 226 (class 1259 OID 16491)
-- Name: equipment; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.equipment (
    equipment_name character varying(100) NOT NULL,
    quantity integer NOT NULL,
    type character varying(50) NOT NULL,
    price_per_hour numeric(10,2) NOT NULL,
    CONSTRAINT equipment_price_per_hour_check CHECK ((price_per_hour >= (0)::numeric)),
    CONSTRAINT equipment_quantity_check CHECK ((quantity >= 0))
);


ALTER TABLE public.equipment OWNER TO postgres;

--
-- TOC entry 227 (class 1259 OID 16500)
-- Name: equipment_rental_info; Type: VIEW; Schema: public; Owner: postgres
--

CREATE VIEW public.equipment_rental_info AS
 SELECT er.rental_number,
    u.first_name,
    u.last_name,
    b.booking_date,
    er.equipment_name,
    er.quantity,
    er.amount
   FROM ((public.equipment_rental er
     JOIN public.bookings b ON ((er.booking_number = b.booking_number)))
     JOIN public.users u ON ((b.user_id = u.user_id)));


ALTER VIEW public.equipment_rental_info OWNER TO postgres;

--
-- TOC entry 228 (class 1259 OID 16505)
-- Name: equipment_rental_rental_number_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public.equipment_rental_rental_number_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.equipment_rental_rental_number_seq OWNER TO postgres;

--
-- TOC entry 5098 (class 0 OID 0)
-- Dependencies: 228
-- Name: equipment_rental_rental_number_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public.equipment_rental_rental_number_seq OWNED BY public.equipment_rental.rental_number;


--
-- TOC entry 229 (class 1259 OID 16506)
-- Name: gyms; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.gyms (
    gym_number integer NOT NULL,
    name character varying(100) NOT NULL,
    table_count integer NOT NULL,
    opening_time time without time zone NOT NULL,
    closing_time time without time zone NOT NULL,
    CONSTRAINT gyms_table_count_check CHECK ((table_count > 0))
);


ALTER TABLE public.gyms OWNER TO postgres;

--
-- TOC entry 230 (class 1259 OID 16515)
-- Name: gyms_gym_number_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public.gyms_gym_number_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.gyms_gym_number_seq OWNER TO postgres;

--
-- TOC entry 5099 (class 0 OID 0)
-- Dependencies: 230
-- Name: gyms_gym_number_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public.gyms_gym_number_seq OWNED BY public.gyms.gym_number;


--
-- TOC entry 231 (class 1259 OID 16516)
-- Name: roles; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.roles (
    id integer NOT NULL,
    role_name character varying(50) NOT NULL
);


ALTER TABLE public.roles OWNER TO postgres;

--
-- TOC entry 232 (class 1259 OID 16521)
-- Name: roles_id_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public.roles_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.roles_id_seq OWNER TO postgres;

--
-- TOC entry 5100 (class 0 OID 0)
-- Dependencies: 232
-- Name: roles_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public.roles_id_seq OWNED BY public.roles.id;


--
-- TOC entry 233 (class 1259 OID 16522)
-- Name: tables; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.tables (
    table_number integer NOT NULL,
    gym_number integer NOT NULL,
    price_per_hour numeric(10,2) NOT NULL,
    CONSTRAINT tables_price_per_hour_check CHECK ((price_per_hour > (0)::numeric))
);


ALTER TABLE public.tables OWNER TO postgres;

--
-- TOC entry 234 (class 1259 OID 16529)
-- Name: tables_table_number_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public.tables_table_number_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.tables_table_number_seq OWNER TO postgres;

--
-- TOC entry 5101 (class 0 OID 0)
-- Dependencies: 234
-- Name: tables_table_number_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public.tables_table_number_seq OWNED BY public.tables.table_number;


--
-- TOC entry 235 (class 1259 OID 16530)
-- Name: tournament_participants; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.tournament_participants (
    user_id integer NOT NULL,
    place integer,
    tournament_id integer,
    CONSTRAINT tournament_participants_place_check CHECK ((place > 0))
);


ALTER TABLE public.tournament_participants OWNER TO postgres;

--
-- TOC entry 236 (class 1259 OID 16535)
-- Name: tournaments; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.tournaments (
    tournament_name character varying(100) NOT NULL,
    organizer character varying(100) NOT NULL,
    participant_count integer,
    date timestamp without time zone NOT NULL,
    tournament_id integer NOT NULL,
    max_participants integer DEFAULT 20 NOT NULL,
    CONSTRAINT tournaments_participant_count_check CHECK ((participant_count >= 0))
);


ALTER TABLE public.tournaments OWNER TO postgres;

--
-- TOC entry 237 (class 1259 OID 16545)
-- Name: tournaments_tournament_id_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public.tournaments_tournament_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.tournaments_tournament_id_seq OWNER TO postgres;

--
-- TOC entry 5102 (class 0 OID 0)
-- Dependencies: 237
-- Name: tournaments_tournament_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public.tournaments_tournament_id_seq OWNED BY public.tournaments.tournament_id;


--
-- TOC entry 238 (class 1259 OID 16546)
-- Name: trainers_trainer_id_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public.trainers_trainer_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.trainers_trainer_id_seq OWNER TO postgres;

--
-- TOC entry 5103 (class 0 OID 0)
-- Dependencies: 238
-- Name: trainers_trainer_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public.trainers_trainer_id_seq OWNED BY public.trainers.trainer_id;


--
-- TOC entry 239 (class 1259 OID 16547)
-- Name: users_user_id_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

CREATE SEQUENCE public.users_user_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE public.users_user_id_seq OWNER TO postgres;

--
-- TOC entry 5104 (class 0 OID 0)
-- Dependencies: 239
-- Name: users_user_id_seq; Type: SEQUENCE OWNED BY; Schema: public; Owner: postgres
--

ALTER SEQUENCE public.users_user_id_seq OWNED BY public.users.user_id;


--
-- TOC entry 4862 (class 2604 OID 16548)
-- Name: bookings booking_number; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.bookings ALTER COLUMN booking_number SET DEFAULT nextval('public.bookings_booking_number_seq'::regclass);


--
-- TOC entry 4864 (class 2604 OID 16549)
-- Name: equipment_rental rental_number; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.equipment_rental ALTER COLUMN rental_number SET DEFAULT nextval('public.equipment_rental_rental_number_seq'::regclass);


--
-- TOC entry 4869 (class 2604 OID 16550)
-- Name: gyms gym_number; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.gyms ALTER COLUMN gym_number SET DEFAULT nextval('public.gyms_gym_number_seq'::regclass);


--
-- TOC entry 4870 (class 2604 OID 16551)
-- Name: roles id; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.roles ALTER COLUMN id SET DEFAULT nextval('public.roles_id_seq'::regclass);


--
-- TOC entry 4871 (class 2604 OID 16552)
-- Name: tables table_number; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.tables ALTER COLUMN table_number SET DEFAULT nextval('public.tables_table_number_seq'::regclass);


--
-- TOC entry 4872 (class 2604 OID 16553)
-- Name: tournaments tournament_id; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.tournaments ALTER COLUMN tournament_id SET DEFAULT nextval('public.tournaments_tournament_id_seq'::regclass);


--
-- TOC entry 4865 (class 2604 OID 16554)
-- Name: trainers trainer_id; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.trainers ALTER COLUMN trainer_id SET DEFAULT nextval('public.trainers_trainer_id_seq'::regclass);


--
-- TOC entry 4866 (class 2604 OID 16555)
-- Name: users user_id; Type: DEFAULT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.users ALTER COLUMN user_id SET DEFAULT nextval('public.users_user_id_seq'::regclass);


--
-- TOC entry 5073 (class 0 OID 16445)
-- Dependencies: 220
-- Data for Name: bookings; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public.bookings (booking_number, user_id, table_number, start_time, total_price, booking_date, coach_id, end_time) FROM stdin;
1	8	2	2025-04-26 13:00:00	1200.00	2025-04-26 00:00:00	\N	2025-04-26 14:00:00
2	3	4	2025-04-26 11:00:00	2900.00	2025-04-26 00:00:00	20	2025-04-26 12:00:00
3	3	4	2025-04-26 12:00:00	2900.00	2025-04-26 00:00:00	20	2025-04-26 13:00:00
4	9	1	2025-04-26 15:00:00	1600.00	2025-04-26 00:00:00	\N	2025-04-26 16:00:00
5	5	5	2025-04-26 15:00:00	2200.00	2025-04-26 00:00:00	19	2025-04-26 16:00:00
6	6	7	2025-04-26 14:00:00	800.00	2025-04-26 00:00:00	\N	2025-04-26 15:00:00
7	6	1	2025-04-26 11:00:00	2200.00	2025-04-26 00:00:00	23	2025-04-26 12:00:00
8	3	3	2025-04-26 13:00:00	2600.00	2025-04-26 00:00:00	\N	2025-04-26 14:00:00
9	3	3	2025-04-26 14:00:00	2600.00	2025-04-26 00:00:00	\N	2025-04-26 15:00:00
10	3	3	2025-04-26 15:00:00	2600.00	2025-04-26 00:00:00	\N	2025-04-26 16:00:00
11	7	6	2025-04-26 12:00:00	1100.00	2025-04-26 00:00:00	\N	2025-04-26 13:00:00
12	7	6	2025-04-26 16:00:00	1100.00	2025-04-26 00:00:00	\N	2025-04-26 17:00:00
14	1	4	2025-06-15 11:00:00	0.00	2025-06-15 00:00:00	\N	2025-06-15 12:00:00
15	1	4	2025-06-15 12:00:00	0.00	2025-06-15 00:00:00	\N	2025-06-15 13:00:00
16	1	4	2025-06-15 13:00:00	0.00	2025-06-15 00:00:00	\N	2025-06-15 14:00:00
17	1	4	2025-06-15 14:00:00	0.00	2025-06-15 00:00:00	\N	2025-06-15 15:00:00
18	1	4	2025-06-15 15:00:00	0.00	2025-06-15 00:00:00	\N	2025-06-15 16:00:00
19	1	4	2025-06-15 16:00:00	0.00	2025-06-15 00:00:00	\N	2025-06-15 17:00:00
20	1	4	2025-06-15 17:00:00	0.00	2025-06-15 00:00:00	\N	2025-06-15 18:00:00
21	1	4	2025-06-15 18:00:00	0.00	2025-06-15 00:00:00	\N	2025-06-15 19:00:00
22	1	4	2025-06-15 19:00:00	0.00	2025-06-15 00:00:00	\N	2025-06-15 20:00:00
23	1	4	2025-06-15 20:00:00	0.00	2025-06-15 00:00:00	\N	2025-06-15 21:00:00
24	1	4	2025-06-15 21:00:00	0.00	2025-06-15 00:00:00	\N	2025-06-15 22:00:00
26	1	5	2025-06-15 11:00:00	0.00	2025-06-15 00:00:00	\N	2025-06-15 12:00:00
27	1	5	2025-06-15 12:00:00	0.00	2025-06-15 00:00:00	\N	2025-06-15 13:00:00
28	1	5	2025-06-15 13:00:00	0.00	2025-06-15 00:00:00	\N	2025-06-15 14:00:00
29	1	5	2025-06-15 14:00:00	0.00	2025-06-15 00:00:00	\N	2025-06-15 15:00:00
30	1	5	2025-06-15 15:00:00	0.00	2025-06-15 00:00:00	\N	2025-06-15 16:00:00
31	1	5	2025-06-15 16:00:00	0.00	2025-06-15 00:00:00	\N	2025-06-15 17:00:00
32	1	5	2025-06-15 17:00:00	0.00	2025-06-15 00:00:00	\N	2025-06-15 18:00:00
33	1	5	2025-06-15 18:00:00	0.00	2025-06-15 00:00:00	\N	2025-06-15 19:00:00
34	1	5	2025-06-15 19:00:00	0.00	2025-06-15 00:00:00	\N	2025-06-15 20:00:00
35	1	5	2025-06-15 20:00:00	0.00	2025-06-15 00:00:00	\N	2025-06-15 21:00:00
36	1	5	2025-06-15 21:00:00	0.00	2025-06-15 00:00:00	\N	2025-06-15 22:00:00
38	1	6	2025-06-15 11:00:00	0.00	2025-06-15 00:00:00	\N	2025-06-15 12:00:00
39	1	6	2025-06-15 12:00:00	0.00	2025-06-15 00:00:00	\N	2025-06-15 13:00:00
40	1	6	2025-06-15 13:00:00	0.00	2025-06-15 00:00:00	\N	2025-06-15 14:00:00
41	1	6	2025-06-15 14:00:00	0.00	2025-06-15 00:00:00	\N	2025-06-15 15:00:00
42	1	6	2025-06-15 15:00:00	0.00	2025-06-15 00:00:00	\N	2025-06-15 16:00:00
43	1	6	2025-06-15 16:00:00	0.00	2025-06-15 00:00:00	\N	2025-06-15 17:00:00
44	1	6	2025-06-15 17:00:00	0.00	2025-06-15 00:00:00	\N	2025-06-15 18:00:00
45	1	6	2025-06-15 18:00:00	0.00	2025-06-15 00:00:00	\N	2025-06-15 19:00:00
46	1	6	2025-06-15 19:00:00	0.00	2025-06-15 00:00:00	\N	2025-06-15 20:00:00
47	1	6	2025-06-15 20:00:00	0.00	2025-06-15 00:00:00	\N	2025-06-15 21:00:00
48	1	6	2025-06-15 21:00:00	0.00	2025-06-15 00:00:00	\N	2025-06-15 22:00:00
50	1	7	2025-06-15 11:00:00	0.00	2025-06-15 00:00:00	\N	2025-06-15 12:00:00
51	1	7	2025-06-15 12:00:00	0.00	2025-06-15 00:00:00	\N	2025-06-15 13:00:00
52	1	7	2025-06-15 13:00:00	0.00	2025-06-15 00:00:00	\N	2025-06-15 14:00:00
53	1	7	2025-06-15 14:00:00	0.00	2025-06-15 00:00:00	\N	2025-06-15 15:00:00
54	1	7	2025-06-15 15:00:00	0.00	2025-06-15 00:00:00	\N	2025-06-15 16:00:00
55	1	7	2025-06-15 16:00:00	0.00	2025-06-15 00:00:00	\N	2025-06-15 17:00:00
56	1	7	2025-06-15 17:00:00	0.00	2025-06-15 00:00:00	\N	2025-06-15 18:00:00
57	1	7	2025-06-15 18:00:00	0.00	2025-06-15 00:00:00	\N	2025-06-15 19:00:00
58	1	7	2025-06-15 19:00:00	0.00	2025-06-15 00:00:00	\N	2025-06-15 20:00:00
59	1	7	2025-06-15 20:00:00	0.00	2025-06-15 00:00:00	\N	2025-06-15 21:00:00
60	1	7	2025-06-15 21:00:00	0.00	2025-06-15 00:00:00	\N	2025-06-15 22:00:00
61	8	4	2025-04-26 13:00:00	900.00	2025-04-26 00:00:00	\N	2025-04-26 14:00:00
211	25	3	2025-05-13 12:00:00	1200.00	2025-05-13 00:00:00	21	2025-05-13 13:00:00
63	25	5	2025-04-26 17:00:00	3700.00	2025-04-26 00:00:00	23	2025-04-26 18:00:00
212	8	5	2025-10-21 16:00:00	1900.00	2025-10-21 00:00:00	19	2025-10-21 17:00:00
65	25	2	2025-04-29 15:00:00	1900.00	2025-04-29 00:00:00	20	2025-04-29 16:00:00
114	1	4	2025-05-10 10:00:00	0.00	2025-05-10 00:00:00	\N	2025-05-10 11:00:00
115	1	4	2025-05-10 11:00:00	0.00	2025-05-10 00:00:00	\N	2025-05-10 12:00:00
116	1	4	2025-05-10 12:00:00	0.00	2025-05-10 00:00:00	\N	2025-05-10 13:00:00
117	1	4	2025-05-10 13:00:00	0.00	2025-05-10 00:00:00	\N	2025-05-10 14:00:00
118	1	4	2025-05-10 14:00:00	0.00	2025-05-10 00:00:00	\N	2025-05-10 15:00:00
119	1	4	2025-05-10 15:00:00	0.00	2025-05-10 00:00:00	\N	2025-05-10 16:00:00
120	1	4	2025-05-10 16:00:00	0.00	2025-05-10 00:00:00	\N	2025-05-10 17:00:00
121	1	4	2025-05-10 17:00:00	0.00	2025-05-10 00:00:00	\N	2025-05-10 18:00:00
122	1	4	2025-05-10 18:00:00	0.00	2025-05-10 00:00:00	\N	2025-05-10 19:00:00
123	1	4	2025-05-10 19:00:00	0.00	2025-05-10 00:00:00	\N	2025-05-10 20:00:00
124	1	4	2025-05-10 20:00:00	0.00	2025-05-10 00:00:00	\N	2025-05-10 21:00:00
125	1	4	2025-05-10 21:00:00	0.00	2025-05-10 00:00:00	\N	2025-05-10 22:00:00
126	1	5	2025-05-10 10:00:00	0.00	2025-05-10 00:00:00	\N	2025-05-10 11:00:00
127	1	5	2025-05-10 11:00:00	0.00	2025-05-10 00:00:00	\N	2025-05-10 12:00:00
128	1	5	2025-05-10 12:00:00	0.00	2025-05-10 00:00:00	\N	2025-05-10 13:00:00
129	1	5	2025-05-10 13:00:00	0.00	2025-05-10 00:00:00	\N	2025-05-10 14:00:00
130	1	5	2025-05-10 14:00:00	0.00	2025-05-10 00:00:00	\N	2025-05-10 15:00:00
131	1	5	2025-05-10 15:00:00	0.00	2025-05-10 00:00:00	\N	2025-05-10 16:00:00
132	1	5	2025-05-10 16:00:00	0.00	2025-05-10 00:00:00	\N	2025-05-10 17:00:00
133	1	5	2025-05-10 17:00:00	0.00	2025-05-10 00:00:00	\N	2025-05-10 18:00:00
134	1	5	2025-05-10 18:00:00	0.00	2025-05-10 00:00:00	\N	2025-05-10 19:00:00
135	1	5	2025-05-10 19:00:00	0.00	2025-05-10 00:00:00	\N	2025-05-10 20:00:00
136	1	5	2025-05-10 20:00:00	0.00	2025-05-10 00:00:00	\N	2025-05-10 21:00:00
137	1	5	2025-05-10 21:00:00	0.00	2025-05-10 00:00:00	\N	2025-05-10 22:00:00
138	1	6	2025-05-10 10:00:00	0.00	2025-05-10 00:00:00	\N	2025-05-10 11:00:00
139	1	6	2025-05-10 11:00:00	0.00	2025-05-10 00:00:00	\N	2025-05-10 12:00:00
140	1	6	2025-05-10 12:00:00	0.00	2025-05-10 00:00:00	\N	2025-05-10 13:00:00
141	1	6	2025-05-10 13:00:00	0.00	2025-05-10 00:00:00	\N	2025-05-10 14:00:00
142	1	6	2025-05-10 14:00:00	0.00	2025-05-10 00:00:00	\N	2025-05-10 15:00:00
143	1	6	2025-05-10 15:00:00	0.00	2025-05-10 00:00:00	\N	2025-05-10 16:00:00
144	1	6	2025-05-10 16:00:00	0.00	2025-05-10 00:00:00	\N	2025-05-10 17:00:00
145	1	6	2025-05-10 17:00:00	0.00	2025-05-10 00:00:00	\N	2025-05-10 18:00:00
146	1	6	2025-05-10 18:00:00	0.00	2025-05-10 00:00:00	\N	2025-05-10 19:00:00
147	1	6	2025-05-10 19:00:00	0.00	2025-05-10 00:00:00	\N	2025-05-10 20:00:00
148	1	6	2025-05-10 20:00:00	0.00	2025-05-10 00:00:00	\N	2025-05-10 21:00:00
149	1	6	2025-05-10 21:00:00	0.00	2025-05-10 00:00:00	\N	2025-05-10 22:00:00
150	1	7	2025-05-10 10:00:00	0.00	2025-05-10 00:00:00	\N	2025-05-10 11:00:00
151	1	7	2025-05-10 11:00:00	0.00	2025-05-10 00:00:00	\N	2025-05-10 12:00:00
152	1	7	2025-05-10 12:00:00	0.00	2025-05-10 00:00:00	\N	2025-05-10 13:00:00
153	1	7	2025-05-10 13:00:00	0.00	2025-05-10 00:00:00	\N	2025-05-10 14:00:00
154	1	7	2025-05-10 14:00:00	0.00	2025-05-10 00:00:00	\N	2025-05-10 15:00:00
155	1	7	2025-05-10 15:00:00	0.00	2025-05-10 00:00:00	\N	2025-05-10 16:00:00
156	1	7	2025-05-10 16:00:00	0.00	2025-05-10 00:00:00	\N	2025-05-10 17:00:00
157	1	7	2025-05-10 17:00:00	0.00	2025-05-10 00:00:00	\N	2025-05-10 18:00:00
158	1	7	2025-05-10 18:00:00	0.00	2025-05-10 00:00:00	\N	2025-05-10 19:00:00
159	1	7	2025-05-10 19:00:00	0.00	2025-05-10 00:00:00	\N	2025-05-10 20:00:00
160	1	7	2025-05-10 20:00:00	0.00	2025-05-10 00:00:00	\N	2025-05-10 21:00:00
161	1	7	2025-05-10 21:00:00	0.00	2025-05-10 00:00:00	\N	2025-05-10 22:00:00
162	23	2	2025-05-13 13:00:00	1300.00	2025-05-13 00:00:00	19	2025-05-13 14:00:00
213	31	1	2026-03-27 12:00:00	1500.00	2026-03-27 00:00:00	\N	2026-03-27 13:00:00
214	31	4	2026-03-27 14:00:00	1500.00	2026-03-27 00:00:00	\N	2026-03-27 15:00:00
215	31	6	2026-03-27 14:00:00	1500.00	2026-03-27 00:00:00	\N	2026-03-27 15:00:00
216	31	7	2026-03-27 14:00:00	1500.00	2026-03-27 00:00:00	\N	2026-03-27 15:00:00
217	31	3	2026-03-27 15:00:00	1500.00	2026-03-27 00:00:00	\N	2026-03-27 16:00:00
223	31	1	2026-03-29 11:00:00	2400.00	2026-03-29 00:00:00	22	2026-03-29 12:00:00
224	31	4	2026-03-29 13:00:00	3300.00	2026-03-29 00:00:00	22	2026-03-29 14:00:00
225	1	3	2026-03-29 13:00:00	7800.00	2026-03-29 00:00:00	24	2026-03-29 14:00:00
226	1	1	2026-03-30 12:00:00	3800.00	2026-03-30 00:00:00	24	2026-03-30 13:00:00
\.


--
-- TOC entry 5078 (class 0 OID 16491)
-- Dependencies: 226
-- Data for Name: equipment; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public.equipment (equipment_name, quantity, type, price_per_hour) FROM stdin;
Ракетка профессиональная	26	Ракетка	600.00
Мячи 30 шт.	20	Мячи	300.00
Мячи 50 шт.	1	Мячи	500.00
Мячи 10 шт.	29	Мячи	100.00
Ракетка любительская	45	Ракетка	200.00
\.


--
-- TOC entry 5074 (class 0 OID 16456)
-- Dependencies: 221
-- Data for Name: equipment_rental; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public.equipment_rental (rental_number, equipment_name, booking_number, quantity, amount) FROM stdin;
1	Ракетка профессиональная	1	1	600.00
2	Мячи 30 шт.	1	1	300.00
5	Ракетка профессиональная	4	2	1200.00
6	Мячи 10 шт.	4	1	100.00
7	Мячи 30 шт.	5	1	300.00
8	Ракетка профессиональная	5	1	600.00
9	Ракетка любительская	6	2	400.00
10	Мячи 10 шт.	6	1	100.00
11	Ракетка профессиональная	7	1	600.00
12	Ракетка профессиональная	10	2	1200.00
13	Мячи 50 шт.	10	1	500.00
16	Ракетка профессиональная	61	1	600.00
17	Мячи 10 шт.	63	1	100.00
18	Мячи 50 шт.	63	1	500.00
19	Ракетка профессиональная	63	2	1200.00
20	Мячи 30 шт.	63	1	300.00
23	Ракетка профессиональная	65	1	600.00
24	Ракетка профессиональная	212	1	600.00
25	Мячи 10 шт.	223	2	200.00
26	Ракетка любительская	223	2	400.00
27	Мячи 30 шт.	224	1	300.00
28	Ракетка профессиональная	224	2	1200.00
29	Мячи 50 шт.	225	9	4500.00
30	Мячи 10 шт.	226	1	100.00
31	Ракетка любительская	226	2	400.00
\.


--
-- TOC entry 5080 (class 0 OID 16506)
-- Dependencies: 229
-- Data for Name: gyms; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public.gyms (gym_number, name, table_count, opening_time, closing_time) FROM stdin;
1	Малый зал	3	10:00:00	21:00:00
2	Главный зал	4	10:00:00	21:00:00
\.


--
-- TOC entry 5082 (class 0 OID 16516)
-- Dependencies: 231
-- Data for Name: roles; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public.roles (id, role_name) FROM stdin;
1	клиент
2	администратор
3	тренер
\.


--
-- TOC entry 5084 (class 0 OID 16522)
-- Dependencies: 233
-- Data for Name: tables; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public.tables (table_number, gym_number, price_per_hour) FROM stdin;
1	1	300.00
2	1	300.00
3	1	300.00
4	2	300.00
5	2	300.00
6	2	300.00
7	2	300.00
\.


--
-- TOC entry 5086 (class 0 OID 16530)
-- Dependencies: 235
-- Data for Name: tournament_participants; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public.tournament_participants (user_id, place, tournament_id) FROM stdin;
2	\N	5
7	\N	5
25	\N	5
31	\N	5
31	\N	7
1	\N	9
\.


--
-- TOC entry 5087 (class 0 OID 16535)
-- Dependencies: 236
-- Data for Name: tournaments; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public.tournaments (tournament_name, organizer, participant_count, date, tournament_id, max_participants) FROM stdin;
Кубок STALIKA	STALIKA	3	2025-06-15 00:00:00	5	40
День Победы	admin	0	2025-05-10 00:00:00	7	50
Первый Областной	Архипов А. И.	0	2026-03-29 00:00:00	9	100
\.


--
-- TOC entry 5075 (class 0 OID 16466)
-- Dependencies: 222
-- Data for Name: trainers; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public.trainers (trainer_id, user_id, hourly_rate, qualification) FROM stdin;
18	11	1200.00	МС
19	17	1000.00	МС
20	19	1000.00	МС
21	20	900.00	МС
22	21	1500.00	МС
23	22	1300.00	КМС
24	31	3000.00	МСМК
\.


--
-- TOC entry 5076 (class 0 OID 16472)
-- Dependencies: 223
-- Data for Name: users; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public.users (user_id, first_name, email, password_hash, role_id, created_at, updated_at, phone, last_name) FROM stdin;
17	Анастасия	as@m.co	c3889c26d3b66a90ff5575eaee30499cf0ce0bb712cbecbbc6acee8e82799df0e06a758501831ca83ddf24f69e21f6de2e175db6a03fac8eb6fdf746213d4878	3	2025-04-25 22:57:19.654669	2025-04-25 22:57:19.654669	+78788582848	Шевцова
19	Елена	yui@g.q	4813c8f818d948475f791039c472ee473c8615668531a01281350e5254f41a15d610838973b3978be894dc64caed7482f9e1fd8c9ed545ea28886acb9134672a	3	2025-04-25 22:59:50.482179	2025-04-25 22:59:50.482179	+71000000000	Иванова
20	Екатерина	mko@za.qw	2f607c369d952d97c8fc28881463dceb845bfab4c33bac85a0135c31abfe89e868e3200a517e9d8426b24432278e50585bef4ae494cc9d290edad2f0fbac6182	3	2025-04-25 23:03:05.800841	2025-04-25 23:03:05.800841	+71234567890	Чебанюк
21	Екатерина	m@m.r	67c1edecc2a0882348f898bf2af74ef8779f1320b3cb12b8f3b13c82294fede97d7b902b3af180869cb6ee67ce0e030ac4dc5e9a48cb08b1433e71f780beb421	3	2025-04-25 23:30:26.806427	2025-04-25 23:30:26.806427	+70009990099	Мещанинова
22	Маргарита	qwerty@m.r	50b74af57bafd19a9bbec2aeb88040e5ceda2a273a58a6285168f8f144f1560ab55d311eaece31c64f49ffc8de78c2bbf7846f07cf454ff6bd8406a93216c7f0	3	2025-04-25 23:33:03.173693	2025-04-25 23:33:03.173693	+76767776677	Михайлова
8	Анжелика	blkaede@gmail.com	bea74b1237aab5d476972b9306e4d89fcfad70e95fac698445fb18a7dc894de88454d482a58ed0e8c8f18b43abc468c62d19b43c8d51196200e3906cd1df0ab3	1	2025-04-25 22:42:29.768171	2025-04-25 23:39:57.249316	+70000000000	Бахматова
23	Валерия	stasushka15@gmail.com	50b74af57bafd19a9bbec2aeb88040e5ceda2a273a58a6285168f8f144f1560ab55d311eaece31c64f49ffc8de78c2bbf7846f07cf454ff6bd8406a93216c7f0	1	2025-04-26 01:06:25.76397	2025-04-26 01:06:25.76397	+74444444444	Сухорукова
25	Глеб	glebodent@mail.ru	50b74af57bafd19a9bbec2aeb88040e5ceda2a273a58a6285168f8f144f1560ab55d311eaece31c64f49ffc8de78c2bbf7846f07cf454ff6bd8406a93216c7f0	1	2025-04-26 20:38:24.335783	2025-04-26 20:38:24.335783	+74546661860	Трофимов
26	Анна	ty@ma	9adc6f0635851019ed18289a572b98f3fd1aab8a0fce9804f9657bd669a2f654cd81945216bdbc7ae68a79f06579c58728ed168c7df899ae49039b2cd213c93c	1	2025-05-13 20:21:36.247799	2025-05-13 20:21:36.247799	+78988488122	Смешнова
27	Стейси	tyuu@f.vv	50b74af57bafd19a9bbec2aeb88040e5ceda2a273a58a6285168f8f144f1560ab55d311eaece31c64f49ffc8de78c2bbf7846f07cf454ff6bd8406a93216c7f0	1	2025-09-29 10:03:52.84774	2025-09-29 10:03:52.84774	+78884448844	Ст
29	апв	st@mail.ru	50b74af57bafd19a9bbec2aeb88040e5ceda2a273a58a6285168f8f144f1560ab55d311eaece31c64f49ffc8de78c2bbf7846f07cf454ff6bd8406a93216c7f0	1	2025-09-29 10:04:43.125854	2025-09-29 10:04:43.125854	+79999999997	вап
2	Иван	ivan85@yandex.ru	774f1878dc17039454671d5347e159fd5dfd65c356fd1f8f05f64b46686d3a866bdebcf4e8b41c9d56352f14e7e9897ac42411e9fff35f4fa2a553e0df95ea17	1	2025-04-25 22:36:01.69935	2025-04-25 22:36:01.69935	+79999999999	Петров
3	Мария	mkuz_003@demo.fake	72fffb6d8a82bd56100b676f58fcd67aed7373d8b8d13191b843010ba3a0f80df828d2afa1c783d0b7153ccc89c5fda8afd6bb814fe303696e1a5c2d2e0d80fe	1	2025-04-25 22:37:34.097902	2025-04-25 22:37:34.097902	+78888888888	Кузнецова
4	Алексей	alex.vor@nothing.now	d36a53842e30cf6c4ec2619542c774d5a178f049f0c78f2872a137955c9c02117a42dd8c666c28f630d216b45ae27c38c4b19a0e861797ff2c4a30248ed4c88a	1	2025-04-25 22:38:17.474549	2025-04-25 22:38:17.474549	+71111111111	Воробьёв
5	Ольга	olsokol_88@zztest.mail	d12756863885892151ef161ab44082afa97863eacfe8a617800d81a47d2fdf4e1645cd14f886173706054b7016fa904679bb9c3a0183224be4ce32b319d801c2	1	2025-04-25 22:39:00.885958	2025-04-25 22:39:00.885958	+72222222222	Соколова
6	Дмитрий	dmitr.pvlv@nowhere.zz	dd9df447312ad836eabfa6d9049d10024a1b1962e91b5a5a00788c098c89dc7afa45870d7002960ab783dd96c9840e97f2969a79d05ba6c82ebaf40e842ffc4b	1	2025-04-25 22:39:36.930602	2025-04-25 22:39:36.930602	+73333333333	Павлов
7	Екатерина	bela_ek23@ghost.zzz	9bb43facd227c682f7b2bdc8e42addb789848b0ca53193ee535fcee02b9aae366cd2e0ea3407e7a8397bbf017025562a3735c98cdfdcbf388ca036390543f6d5	1	2025-04-25 22:40:20.456284	2025-04-25 22:40:20.456284	+75555555555	Белова
9	Юлия	julia.akula.gus@gmail.com	451685cacbc096e0c574b2ae89a7ef04b49f611707c3ec05b0b049a1dc16743453224a0e83443e2b18f582d953b9585154a8ef0c1a8f15865586101838fcc502	1	2025-04-25 22:44:44.157727	2025-04-25 22:44:44.157727	+74554545544	Гусева
10	Инга	ingashtern@mail.ru	3dca4d9b6923a3b03b69f4dcbab017c9ec87408392ca94efc0a409b28b1931058e9345236932aa3088350a27359cc1660a4b522e7ced70cb19958fb340deccd3	1	2025-04-25 22:46:00.328308	2025-04-25 22:46:00.328308	+79998987766	Штерн
11	Егор	aligrig77@nonhost.zz	ea236d057e09d35d675e65629485688407e10d79db891111226443aab43138d1a110dc852ae0a5414365b5abf25ea525d92843c3b3688e47ef220b6fe1edb2dc	3	2025-04-25 22:48:25.843134	2025-04-25 22:48:25.843134	+71233232323	Трушкин
30	aboba	sdfsdfsdfsdfg@mail.ru	bd5bfde762add50b971c1ce1cfc3853c42ba94277090cb0f70de2c081e560717089250f42e1d61c0043846f3b4cb0443dda1392b57e87a052507dac2cb6a187d	1	2026-03-27 14:51:23.555359	2026-03-27 14:51:23.555359	+79003002434	boy
32	Денис	fsfsdfsdfsdf@mail.ru	9e20c4e84b1de09983669ffa5de1fccd574698ac0b2e062d408e5ea1d19116067ab8fb4f3f5b9829a3107ad95ba35474a8393a2c386ee60cc84ef07fd5ea0bd7	1	2026-03-29 18:46:18.287519	2026-03-29 18:46:18.287579	89993332323	Штормов
1	Станислава	stasi-06@mail.ru	7fcf4ba391c48784edde599889d6e3f1e47a27db36ecc050cc92f259bfac38afad2c68a1ae804d77075e8fb722503f3eca2b2c1006ee6f6c7b7628cb45fffd1d	2	2025-04-25 22:23:29.367425	2026-03-29 19:20:17.05678	+79819504929	Трофимова
31	Виктор	AbobaBoychik@mail.ru	ce2e9987c25334fbb932370fceda7faee31e17b78328afcb70e5e128723622c78df051783c721dccb188b9bbb5f58fb46914d58cb961fdff856478b748929d8f	3	2026-03-27 14:53:36.406898	2026-03-29 20:15:05.404774	+79002003434	Викторович
\.


--
-- TOC entry 5105 (class 0 OID 0)
-- Dependencies: 225
-- Name: bookings_booking_number_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public.bookings_booking_number_seq', 226, true);


--
-- TOC entry 5106 (class 0 OID 0)
-- Dependencies: 228
-- Name: equipment_rental_rental_number_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public.equipment_rental_rental_number_seq', 31, true);


--
-- TOC entry 5107 (class 0 OID 0)
-- Dependencies: 230
-- Name: gyms_gym_number_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public.gyms_gym_number_seq', 1, false);


--
-- TOC entry 5108 (class 0 OID 0)
-- Dependencies: 232
-- Name: roles_id_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public.roles_id_seq', 1, false);


--
-- TOC entry 5109 (class 0 OID 0)
-- Dependencies: 234
-- Name: tables_table_number_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public.tables_table_number_seq', 1, false);


--
-- TOC entry 5110 (class 0 OID 0)
-- Dependencies: 237
-- Name: tournaments_tournament_id_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public.tournaments_tournament_id_seq', 9, true);


--
-- TOC entry 5111 (class 0 OID 0)
-- Dependencies: 238
-- Name: trainers_trainer_id_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public.trainers_trainer_id_seq', 24, true);


--
-- TOC entry 5112 (class 0 OID 0)
-- Dependencies: 239
-- Name: users_user_id_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public.users_user_id_seq', 32, true);


--
-- TOC entry 4885 (class 2606 OID 16557)
-- Name: bookings bookings_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.bookings
    ADD CONSTRAINT bookings_pkey PRIMARY KEY (booking_number);


--
-- TOC entry 4899 (class 2606 OID 16559)
-- Name: equipment equipment_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.equipment
    ADD CONSTRAINT equipment_pkey PRIMARY KEY (equipment_name);


--
-- TOC entry 4887 (class 2606 OID 16561)
-- Name: equipment_rental equipment_rental_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.equipment_rental
    ADD CONSTRAINT equipment_rental_pkey PRIMARY KEY (rental_number);


--
-- TOC entry 4901 (class 2606 OID 16563)
-- Name: gyms gyms_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.gyms
    ADD CONSTRAINT gyms_pkey PRIMARY KEY (gym_number);


--
-- TOC entry 4903 (class 2606 OID 16565)
-- Name: roles roles_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.roles
    ADD CONSTRAINT roles_pkey PRIMARY KEY (id);


--
-- TOC entry 4905 (class 2606 OID 16567)
-- Name: roles roles_role_name_key; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.roles
    ADD CONSTRAINT roles_role_name_key UNIQUE (role_name);


--
-- TOC entry 4907 (class 2606 OID 16569)
-- Name: tables tables_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.tables
    ADD CONSTRAINT tables_pkey PRIMARY KEY (table_number);


--
-- TOC entry 4909 (class 2606 OID 16571)
-- Name: tournaments tournaments_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.tournaments
    ADD CONSTRAINT tournaments_pkey PRIMARY KEY (tournament_id);


--
-- TOC entry 4889 (class 2606 OID 16573)
-- Name: trainers trainers_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.trainers
    ADD CONSTRAINT trainers_pkey PRIMARY KEY (trainer_id);


--
-- TOC entry 4891 (class 2606 OID 16575)
-- Name: users unique_email; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.users
    ADD CONSTRAINT unique_email UNIQUE (email);


--
-- TOC entry 4893 (class 2606 OID 16577)
-- Name: users unique_phone; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.users
    ADD CONSTRAINT unique_phone UNIQUE (phone);


--
-- TOC entry 4895 (class 2606 OID 16579)
-- Name: users users_email_key; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.users
    ADD CONSTRAINT users_email_key UNIQUE (email);


--
-- TOC entry 4897 (class 2606 OID 16581)
-- Name: users users_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.users
    ADD CONSTRAINT users_pkey PRIMARY KEY (user_id);


--
-- TOC entry 4921 (class 2620 OID 16582)
-- Name: equipment_rental check_inventory_quantity_trigger; Type: TRIGGER; Schema: public; Owner: postgres
--

CREATE TRIGGER check_inventory_quantity_trigger BEFORE INSERT ON public.equipment_rental FOR EACH ROW EXECUTE FUNCTION public.check_inventory_quantity();


--
-- TOC entry 4923 (class 2620 OID 16583)
-- Name: tournament_participants check_tournament_participants_trigger; Type: TRIGGER; Schema: public; Owner: postgres
--

CREATE TRIGGER check_tournament_participants_trigger BEFORE INSERT ON public.tournament_participants FOR EACH ROW EXECUTE FUNCTION public.check_tournament_participants();


--
-- TOC entry 4922 (class 2620 OID 16584)
-- Name: equipment_rental update_inventory_on_cancellation_trigger; Type: TRIGGER; Schema: public; Owner: postgres
--

CREATE TRIGGER update_inventory_on_cancellation_trigger BEFORE DELETE ON public.equipment_rental FOR EACH ROW EXECUTE FUNCTION public.update_inventory_on_cancellation();


--
-- TOC entry 4910 (class 2606 OID 16585)
-- Name: bookings bookings_coach_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.bookings
    ADD CONSTRAINT bookings_coach_id_fkey FOREIGN KEY (coach_id) REFERENCES public.trainers(trainer_id) ON DELETE SET NULL;


--
-- TOC entry 4911 (class 2606 OID 16590)
-- Name: bookings bookings_table_number_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.bookings
    ADD CONSTRAINT bookings_table_number_fkey FOREIGN KEY (table_number) REFERENCES public.tables(table_number);


--
-- TOC entry 4912 (class 2606 OID 16595)
-- Name: bookings bookings_user_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.bookings
    ADD CONSTRAINT bookings_user_id_fkey FOREIGN KEY (user_id) REFERENCES public.users(user_id) ON DELETE CASCADE;


--
-- TOC entry 4914 (class 2606 OID 16600)
-- Name: equipment_rental equipment_rental_booking_number_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.equipment_rental
    ADD CONSTRAINT equipment_rental_booking_number_fkey FOREIGN KEY (booking_number) REFERENCES public.bookings(booking_number) ON DELETE CASCADE;


--
-- TOC entry 4915 (class 2606 OID 16605)
-- Name: equipment_rental equipment_rental_equipment_name_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.equipment_rental
    ADD CONSTRAINT equipment_rental_equipment_name_fkey FOREIGN KEY (equipment_name) REFERENCES public.equipment(equipment_name) ON DELETE CASCADE;


--
-- TOC entry 4913 (class 2606 OID 16610)
-- Name: bookings fk_user_id; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.bookings
    ADD CONSTRAINT fk_user_id FOREIGN KEY (user_id) REFERENCES public.users(user_id);


--
-- TOC entry 4918 (class 2606 OID 16615)
-- Name: tables tables_gym_number_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.tables
    ADD CONSTRAINT tables_gym_number_fkey FOREIGN KEY (gym_number) REFERENCES public.gyms(gym_number) ON DELETE CASCADE;


--
-- TOC entry 4919 (class 2606 OID 16620)
-- Name: tournament_participants tournament_participants_tournament_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.tournament_participants
    ADD CONSTRAINT tournament_participants_tournament_id_fkey FOREIGN KEY (tournament_id) REFERENCES public.tournaments(tournament_id) ON DELETE CASCADE;


--
-- TOC entry 4920 (class 2606 OID 16625)
-- Name: tournament_participants tournament_participants_user_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.tournament_participants
    ADD CONSTRAINT tournament_participants_user_id_fkey FOREIGN KEY (user_id) REFERENCES public.users(user_id);


--
-- TOC entry 4916 (class 2606 OID 16630)
-- Name: trainers trainers_user_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.trainers
    ADD CONSTRAINT trainers_user_id_fkey FOREIGN KEY (user_id) REFERENCES public.users(user_id) ON DELETE CASCADE;


--
-- TOC entry 4917 (class 2606 OID 16635)
-- Name: users users_role_id_fkey; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public.users
    ADD CONSTRAINT users_role_id_fkey FOREIGN KEY (role_id) REFERENCES public.roles(id) ON DELETE CASCADE;


-- Completed on 2026-03-29 22:09:28

--
-- PostgreSQL database dump complete
--

\unrestrict JRWygu3zr8cST5qokgJE3GIl0JOi5cjM1QsiVYxVJ2PIBU34jzSvIugIbjHhHrk

