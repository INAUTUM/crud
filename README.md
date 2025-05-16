# crud

Web API сервис на .NET 9, реализующий API методы CRUD над сущностью Users, доступ к API осуществляется через интерфейс Swagger. Все действия происходят в оперативной памяти и не сохраняются при остановке api. Предварительно создан пользователь Admin, от имени которого будут вначале происходить действия.

## Admin пользователь
- Логин: `admin`
- Пароль: `admin123`

## Как использовать

1. Скачать проект и перейти в директорию с проектом:
```bash
git clone https://github.com/INAUTUM/crud.git
cd crud
```
2. Запустить проект.
```bash
dotnet run
```
3. В `POST /Auth/login` нажать `Try in out` и ввести логин и пароль админа, после нажать `Execute`.
4. В ответе получить JWT-токен и сохранить его в заголовке `Authorization` в запросах к API.


## Полное описание методов API

### **1. POST /Auth/login**
**Назначение:** Аутентификация пользователя и получение JWT-токена.  
**Как использовать:**  
```http
POST /Auth/login
Content-Type: application/json

{
  "login": "user123",
  "password": "qwerty123"
}
```
**Ответ:**  
```json
{
  "token": "ey..."
}
```
**Как работает:**  
- Проверяет логин и пароль.
- Если пользователь активен (`RevokedOn == null`), генерирует JWT-токен с ролью (`Admin` или `User`).
- Можно добавить срок действия токена для большей безопасности.

---

### **2. POST /api/Users**
**Назначение:** Создание нового пользователя (только для администраторов).  
**Права:** `Admin`  
**Как использовать:**  
```http
POST /api/Users
Authorization: Bearer <ADMIN_TOKEN>
Content-Type: application/json

{
  "login": "new_user",
  "password": "password123",
  "name": "Иван Иванов",
  "gender": 1,
  "birthday": "2000-01-01",
  "admin": false
}
```
**Ответ:**  
- `201 Created` с данными созданного пользователя.  
- `400 Bad Request` при невалидных данных или дубликате логина.

**Как работает:**  
- Валидирует входные данные (логин, пароль, дата рождения).
- Проверяет уникальность логина.
- Автоматически заполняет поля:  
  `CreatedOn`, `CreatedBy`, `ModifiedOn`, `ModifiedBy`.

---

### **3. PUT /api/Users/{login}/details**
**Назначение:** Изменение данных пользователя (имя, пол, дата рождения).  
**Права:**  
- `Admin` (может менять любого пользователя).  
- `User` (только свои данные, если активен).  

**Как использовать:**  
```http
PUT /api/Users/user123/details
Authorization: Bearer <TOKEN>
Content-Type: application/json

{
  "name": "Новое имя",
  "gender": 0,
  "birthday": "2005-05-05"
}
```
**Ответ:**  
- `200 OK` с обновленными данными.  
- `403 Forbidden` при попытке изменить чужой профиль.  

**Как работает:**  
- Проверяет, что пользователь активен (`RevokedOn == null`).  
- Обновляет поля: `Name`, `Gender`, `Birthday`.  
- Автоматически обновляет `ModifiedOn` и `ModifiedBy`.

---

### **4. PUT /api/Users/{login}/password**
**Назначение:** Смена пароля.  
**Права:**  
- `Admin` (может менять пароль без старого).  
- `User` (требуется старый пароль, если активен).  

**Как использовать:**  
```http
PUT /api/Users/user123/password
Authorization: Bearer <TOKEN>
Content-Type: application/json

{
  "oldPassword": "old_pass", // Не требуется для админа
  "newPassword": "new_pass"
}
```
**Ответ:**  
- `200 OK` при успехе.  
- `400 Bad Request` при неверном старом пароле.  

**Как работает:**  
- Для пользователей: проверяет `oldPassword`.  
- Для админов: изменение без проверки старого пароля.  
- Хеширует новый пароль (если настроено).

---

### **5. PUT /api/Users/{login}/login**
**Назначение:** Изменение логина.  
**Права:**  
- `Admin` (может менять любой логин).  
- `User` (только свой логин, если активен).  

**Как использовать:**  
```http
PUT /api/Users/user123/login
Authorization: Bearer <TOKEN>
Content-Type: application/json

{
  "newLogin": "new_user123"
}
```
**Ответ:**  
- `200 OK` с новым логином.  
- `400 Bad Request` при дубликате логина.  

**Как работает:**  
- Проверяет уникальность нового логина.  
- Обновляет `Login` и `ModifiedOn/ModifiedBy`.

---

### **6. GET /api/Users/{login}**
**Назначение:** Получение полной информации о пользователе.  
**Права:** `Admin`  
**Как использовать:**  
```http
GET /api/Users/user123
Authorization: Bearer <ADMIN_TOKEN>
```
**Ответ:**  
```json
{
  "login": "user123",
  "name": "Иван Иванов",
  "gender": 1,
  "birthday": "2000-01-01",
  "admin": false,
  "isActive": true
}
```

---

### **7. DELETE /api/Users/{login}**
**Назначение:** Удаление пользователя (мягкое или полное).  
**Права:** `Admin`  
**Параметры:**  
- `softDelete=true` (по умолчанию) — мягкое удаление.  
- `softDelete=false` — полное удаление.  

**Как использовать:**  
```http
DELETE /api/Users/user123?softDelete=true
Authorization: Bearer <ADMIN_TOKEN>
```
**Как работает:**  
- Мягкое удаление: устанавливает `RevokedOn` и `RevokedBy`.  
- Полное удаление: удаляет запись.

---

### **8. GET /api/Users/me**
**Назначение:** Получение информации о текущем пользователе.  
**Права:** Любой авторизованный пользователь.  
**Как использовать:**  
```http
GET /api/Users/me
Authorization: Bearer <USER_TOKEN>
```
**Ответ:**  
- `200 OK` с данными пользователя.  
- `401 Unauthorized` если пользователь отозван (`RevokedOn != null`).

---

### **9. GET /api/Users/older-than/{age}**
**Назначение:** Получение пользователей старше указанного возраста.  
**Права:** `Admin`  
**Как использовать:**  
```http
GET /api/Users/older-than/18
Authorization: Bearer <ADMIN_TOKEN>
```
**Ответ:**  
- Список пользователей, чей возраст > `age` (на основе `Birthday`).

---

### **10. POST /api/Users/{login}/restore**
**Назначение:** Восстановление отозванного пользователя.  
**Права:** `Admin`  
**Как использовать:**  
```http
POST /api/Users/user123/restore
Authorization: Bearer <ADMIN_TOKEN>
```
**Как работает:**  
- Сбрасывает поля `RevokedOn` и `RevokedBy`.

---

### **11. GET /api/Users/active**
**Назначение:** Получение списка активных пользователей.  
**Права:** `Admin`  
**Как использовать:**  
```http
GET /api/Users/active
Authorization: Bearer <ADMIN_TOKEN>
```
**Ответ:**  
- Список пользователей с `RevokedOn == null`, отсортированный по `CreatedOn`.

---

## Общие принципы работы:
1. **Авторизация:** Требуется JWT-токен в заголовке `Authorization` (кроме `/Auth/login`). 
2. **Валидация:**  
   - Логин/пароль: только латинские буквы, цифры и `_`.  
   - Дата рождения: формат `YYYY-MM-DD`.  
3. **Обработка ошибок:**  
   - `400 Bad Request`: невалидные данные.  
   - `401 Unauthorized`: неавторизованный доступ.  
   - `403 Forbidden`: недостаточно прав.  
   - `404 Not Found`: пользователь не существует.  

Работы с API - [Swagger UI](http://localhost:5179/swagger).