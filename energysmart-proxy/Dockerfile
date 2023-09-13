FROM nginx:1.24

RUN apt-get update && apt-get install -y openssl

COPY nginx.conf.template .

RUN openssl req  -nodes -new -x509 -sha1 -subj '/CN=energysmartwaterheater.com' -keyout /etc/nginx/energysmartwaterheater.com.key -out /etc/nginx/energysmartwaterheater.com.crt -days 3650

EXPOSE 443/tcp

CMD ["/bin/sh" , "-c" , "envsubst < /nginx.conf.template > /etc/nginx/nginx.conf && exec nginx -g 'daemon off;'"]
