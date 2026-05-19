FROM nginx:alpine

COPY public /usr/share/nginx/html/

EXPOSE 80

HEALTHCHECK --interval=10s --timeout=5s --retries=3 --start-period=10s \
    CMD wget --quiet --tries=1 --spider http://localhost/health || exit 1
