# NspdWebService
Сервис по получению данных об объектах из <b>[Портал пространственных данных Национальная система пространственных данных](https://nspd.gov.ru/)</b></br></br>
<img width="1715" height="965" alt="preview" src="https://github.com/user-attachments/assets/18d93df0-e2d0-43ad-b09b-47ee09ed07fc" />

<h1>Поиск</h1>
Поиск можно выполнять через интерфейс или используя адресную строку:</br>
http{s}://{ip}:{port}/Map?handler=Search?{searchType}?{number}</br></br>

<b>Примеры:</b></br>
http://localhost:5000/Map</br>
http://localhost:5000/Map?handler=Search?1?23:14:0000000:1107</br>
http://localhost:5000/Map?handler=Search?7?09:07-7.108

<h2>Типы поиска (searchType)</h2>
1 - Поиск объектов недвижимости.</br>
2 - Поиск по кадастровому делению.</br>
4 - Поиск административно-территориальных единиц.</br>
5 - Поиск зон и территорий (ЗОУИТ).</br>
7 - Поиск территориальных зон.</br>
