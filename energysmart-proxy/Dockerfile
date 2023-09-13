FROM nginx:1.24

RUN apt-get update && apt-get install -y openssl

COPY nginx.conf /etc/nginx/

RUN openssl req  -nodes -new -x509 -sha1 -subj '/CN=energysmartwaterheater.com' -keyout /etc/nginx/energysmartwaterheater.com.key -out /etc/nginx/energysmartwaterheater.com.crt -days 3650

EXPOSE 443/tcp

CMD ["nginx", "-g", "daemon off;"]
